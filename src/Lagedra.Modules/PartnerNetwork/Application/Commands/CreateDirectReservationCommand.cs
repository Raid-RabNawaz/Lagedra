using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

/// <summary>
/// Creates a <see cref="DirectReservation"/> AND a real <c>DealApplication</c>
/// (via the SharedKernel <see cref="IPartnerDirectBookingService"/>) for a guest
/// who already has a Lagedra account (Phase 18.7).
///
/// If the guest does NOT have an account yet, the partner must call
/// <c>POST /v1/partners/{id}/invites</c> first (which creates the user + reservation
/// in one shot) — the standalone reservation endpoint deliberately does NOT auto-invite
/// to keep the two responsibilities separable.
///
/// Authorization: caller must be a verified-org admin via
/// <see cref="IPartnerAccessService.RequireVerifiedOrgAdminAsync"/>.
/// </summary>
public sealed record CreateDirectReservationCommand(
    Guid OrganizationId,
    string GuestName,
    string GuestEmail,
    Guid ListingId,
    Guid ReservedByUserId,
    bool ReservedByIsPlatformAdmin,
    DateOnly? RequestedCheckIn = null,
    DateOnly? RequestedCheckOut = null) : IRequest<Result<DirectReservationConversionDto>>;

public sealed record DirectReservationConversionDto(
    DirectReservationDto Reservation,
    PartnerDirectBookingResult DealApplication,
    bool TruthSurfacePending);

public sealed class CreateDirectReservationCommandHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IUserLookupService userLookup,
    IPartnerDirectBookingService bookingService,
    IClock clock)
    : IRequestHandler<CreateDirectReservationCommand, Result<DirectReservationConversionDto>>
{
    private static readonly Error GuestNotInvited = new(
        "Reservation.GuestNotInvited",
        "No Lagedra account exists for that guest email. Use POST /v1/partners/{id}/invites first to create the user account.");

    public async Task<Result<DirectReservationConversionDto>> Handle(
        CreateDirectReservationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authzResult = await accessService.RequireVerifiedOrgAdminAsync(
            request.ReservedByUserId,
            request.OrganizationId,
            request.ReservedByIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result<DirectReservationConversionDto>.Failure(authzResult.Error);
        }

        var tenantUserId = await userLookup
            .FindUserIdByEmailAsync(request.GuestEmail, cancellationToken)
            .ConfigureAwait(false);

        if (tenantUserId is null)
        {
            return Result<DirectReservationConversionDto>.Failure(GuestNotInvited);
        }

        var (checkIn, checkOut) = ResolveDates(request);

        var bookingResult = await bookingService.SubmitAsync(
            new PartnerDirectBookingRequest(
                request.ListingId,
                tenantUserId.Value,
                request.OrganizationId,
                checkIn,
                checkOut),
            cancellationToken).ConfigureAwait(false);

        if (bookingResult.IsFailure)
        {
            return Result<DirectReservationConversionDto>.Failure(bookingResult.Error);
        }

        var reservation = DirectReservation.Create(
            request.OrganizationId, request.GuestName, request.GuestEmail,
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

    /// <summary>
    /// Defaults: when the partner does not specify dates, book a 30-day stay starting
    /// 7 days from today (UTC). Listing-level min/max-stay is still enforced inside
    /// the booking service.
    /// </summary>
    private (DateOnly CheckIn, DateOnly CheckOut) ResolveDates(CreateDirectReservationCommand request)
    {
        var today = DateOnly.FromDateTime(clock.UtcNow.Date);
        var defaultCheckIn = today.AddDays(7);
        var defaultCheckOut = defaultCheckIn.AddDays(30);

        return (request.RequestedCheckIn ?? defaultCheckIn,
                request.RequestedCheckOut ?? defaultCheckOut);
    }
}
