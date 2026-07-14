using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

/// <summary>
/// Creates a <see cref="DirectReservation"/> AND a real <c>DealApplication</c>
/// for an endorsed member of the partner organization.
///
/// The member must already have an Approved endorsement for this org.
/// Invite + endorse first via <c>InvitePartnerGuestCommand</c> when needed.
/// </summary>
public sealed record CreateDirectReservationCommand(
    Guid OrganizationId,
    Guid TenantUserId,
    Guid ListingId,
    string PayerType,
    Guid ReservedByUserId,
    bool ReservedByIsPlatformAdmin,
    DateOnly? RequestedCheckIn = null,
    DateOnly? RequestedCheckOut = null,
    string? StripePaymentMethodId = null) : IRequest<Result<DirectReservationConversionDto>>;

public sealed record DirectReservationConversionDto(
    DirectReservationDto Reservation,
    PartnerDirectBookingResult DealApplication,
    bool TruthSurfacePending);

public sealed class CreateDirectReservationCommandHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IUserDirectoryService userDirectory,
    IPartnerDirectBookingService bookingService,
    IClock clock)
    : IRequestHandler<CreateDirectReservationCommand, Result<DirectReservationConversionDto>>
{
    private static readonly Error MemberRequired = new(
        "Reservation.MemberRequired",
        "Select an endorsed member before creating a reservation.");

    private static readonly Error MemberNotEndorsed = new(
        "Reservation.MemberNotEndorsed",
        "You can only book for members with an approved endorsement from your organization.");

    private static readonly Error InvalidPayerType = new(
        "Reservation.InvalidPayerType",
        "Payer type must be Tenant or PartnerOrganization.");

    private static readonly Error PartnerPaymentRequired = new(
        "Reservation.PartnerPaymentRequired",
        "Attach a company payment method when the partner organization pays.");

    public async Task<Result<DirectReservationConversionDto>> Handle(
        CreateDirectReservationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TenantUserId == Guid.Empty)
        {
            return Result<DirectReservationConversionDto>.Failure(MemberRequired);
        }

        if (!TryParsePayerType(request.PayerType, out var payerType))
        {
            return Result<DirectReservationConversionDto>.Failure(InvalidPayerType);
        }

        if (payerType == PartnerDirectBookingPayerType.PartnerOrganization
            && string.IsNullOrWhiteSpace(request.StripePaymentMethodId))
        {
            return Result<DirectReservationConversionDto>.Failure(PartnerPaymentRequired);
        }

        var authzResult = await accessService.RequireVerifiedOrgAdminAsync(
            request.ReservedByUserId,
            request.OrganizationId,
            request.ReservedByIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result<DirectReservationConversionDto>.Failure(authzResult.Error);
        }

        var endorsed = await dbContext.Endorsements
            .AsNoTracking()
            .AnyAsync(
                e => e.OrganizationId == request.OrganizationId
                  && e.TenantUserId == request.TenantUserId
                  && e.Status == PartnerEndorsementStatus.Approved,
                cancellationToken)
            .ConfigureAwait(false);

        if (!endorsed)
        {
            return Result<DirectReservationConversionDto>.Failure(MemberNotEndorsed);
        }

        var directory = await userDirectory
            .GetEntriesAsync([request.TenantUserId], cancellationToken)
            .ConfigureAwait(false);

        if (!directory.TryGetValue(request.TenantUserId, out var member))
        {
            return Result<DirectReservationConversionDto>.Failure(MemberNotEndorsed);
        }

        var invite = await dbContext.GuestInvites
            .AsNoTracking()
            .Where(i => i.OrganizationId == request.OrganizationId
                     && i.InvitedUserId == request.TenantUserId)
            .OrderByDescending(i => i.InvitedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var guestName = !string.IsNullOrWhiteSpace(invite?.FullName)
            ? invite!.FullName
            : member.DisplayName;
        var guestEmail = !string.IsNullOrWhiteSpace(member.Email)
            ? member.Email
            : invite?.Email ?? string.Empty;

        if (string.IsNullOrWhiteSpace(guestEmail))
        {
            return Result<DirectReservationConversionDto>.Failure(
                new Error("Reservation.MemberEmailMissing",
                    "Could not resolve an email for the selected member."));
        }

        var (checkIn, checkOut) = ResolveDates(request);

        var bookingResult = await bookingService.SubmitAsync(
            new PartnerDirectBookingRequest(
                request.ListingId,
                request.TenantUserId,
                request.OrganizationId,
                checkIn,
                checkOut,
                payerType,
                request.ReservedByUserId,
                request.StripePaymentMethodId),
            cancellationToken).ConfigureAwait(false);

        if (bookingResult.IsFailure)
        {
            return Result<DirectReservationConversionDto>.Failure(bookingResult.Error);
        }

        var reservation = DirectReservation.Create(
            request.OrganizationId, guestName, guestEmail,
            request.ListingId, request.ReservedByUserId, clock);

        reservation.LinkDealApplication(bookingResult.Value.ApplicationId, clock);

        dbContext.DirectReservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DirectReservationConversionDto>.Success(new DirectReservationConversionDto(
            new DirectReservationDto(reservation.Id, reservation.OrganizationId,
                reservation.GuestName, reservation.GuestEmail, reservation.ListingId,
                reservation.DealApplicationId, reservation.ReservedByUserId, reservation.CreatedAt),
            bookingResult.Value,
            TruthSurfacePending: true));
    }

    private (DateOnly CheckIn, DateOnly CheckOut) ResolveDates(CreateDirectReservationCommand request)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.Date);
        var defaultCheckIn = today.AddDays(7);
        var defaultCheckOut = defaultCheckIn.AddDays(30);

        return (request.RequestedCheckIn ?? defaultCheckIn,
                request.RequestedCheckOut ?? defaultCheckOut);
    }

    private static bool TryParsePayerType(string? raw, out PartnerDirectBookingPayerType payerType)
    {
        if (string.Equals(raw, "Tenant", StringComparison.OrdinalIgnoreCase))
        {
            payerType = PartnerDirectBookingPayerType.Tenant;
            return true;
        }

        if (string.Equals(raw, "PartnerOrganization", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "Partner", StringComparison.OrdinalIgnoreCase))
        {
            payerType = PartnerDirectBookingPayerType.PartnerOrganization;
            return true;
        }

        payerType = PartnerDirectBookingPayerType.Tenant;
        return false;
    }
}
