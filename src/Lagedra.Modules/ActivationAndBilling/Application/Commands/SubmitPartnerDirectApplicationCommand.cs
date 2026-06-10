using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// Submits a <see cref="DealApplication"/> on behalf of a tenant, attributed to a
/// specific partner organization. Only invoked from inside <c>CreateDirectReservationCommand</c>
/// (Phase 18.7) — there is no public endpoint that maps directly to this command.
///
/// Differences vs <see cref="SubmitApplicationCommand"/>:
///   - The caller is the partner, not the tenant. Tenant id is supplied directly.
///   - <see cref="DealApplication.Source"/> is set to <see cref="DealApplicationSource.PartnerDirectReservation"/>.
///   - The listing must have <see cref="ListingDetailsDto.AcceptsPartnerDirectReservations"/> = true.
/// </summary>
public sealed record SubmitPartnerDirectApplicationCommand(
    Guid ListingId,
    Guid TenantUserId,
    Guid PartnerOrganizationId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut) : IRequest<Result<DealApplicationDto>>;

public sealed class SubmitPartnerDirectApplicationCommandHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider)
    : IRequestHandler<SubmitPartnerDirectApplicationCommand, Result<DealApplicationDto>>
{
    private static readonly Error ListingNotFound =
        new("Listing.NotFound", "Listing not found.");
    private static readonly Error OptedOut =
        new("Listing.PartnerDirectReservationsNotAccepted",
            "This listing does not accept partner direct reservations.");
    private static readonly Error DatesOutOfRange =
        new("Dates.OutOfStayRange", "Requested dates fall outside the listing's allowed stay range.");
    private static readonly Error DatesUnavailable =
        new("Dates.Unavailable", "The requested dates are not available.");
    private static readonly Error OwnListing =
        new("Application.OwnListing", "You cannot apply to your own listing.");

    public async Task<Result<DealApplicationDto>> Handle(
        SubmitPartnerDirectApplicationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await listingProvider
            .GetListingDetailsAsync(request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<DealApplicationDto>.Failure(ListingNotFound);
        }

        if (!listing.AcceptsPartnerDirectReservations)
        {
            return Result<DealApplicationDto>.Failure(OptedOut);
        }

        if (listing.LandlordUserId == request.TenantUserId)
        {
            return Result<DealApplicationDto>.Failure(OwnListing);
        }

        var duration = request.RequestedCheckOut.DayNumber - request.RequestedCheckIn.DayNumber;

        if ((listing.MinStayDays.HasValue && duration < listing.MinStayDays.Value) ||
            (listing.MaxStayDays.HasValue && duration > listing.MaxStayDays.Value))
        {
            return Result<DealApplicationDto>.Failure(DatesOutOfRange);
        }

        var isAvailable = await listingProvider
            .IsAvailableAsync(request.ListingId, request.RequestedCheckIn, request.RequestedCheckOut, cancellationToken)
            .ConfigureAwait(false);
        if (!isAvailable)
        {
            return Result<DealApplicationDto>.Failure(DatesUnavailable);
        }

        var application = DealApplication.Submit(
            request.ListingId,
            request.TenantUserId,
            listing.LandlordUserId,
            request.RequestedCheckIn,
            request.RequestedCheckOut,
            partnerOrganizationId: request.PartnerOrganizationId,
            isPartnerReferred: true,
            source: DealApplicationSource.PartnerDirectReservation);

        dbContext.DealApplications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DealApplicationDto>.Success(MapToDto(application));
    }

    private static DealApplicationDto MapToDto(DealApplication a) =>
        new(a.Id, a.ListingId, a.TenantUserId, a.LandlordUserId,
            a.Status, a.DealId, a.SubmittedAt, a.DecidedAt,
            a.RequestedCheckIn, a.RequestedCheckOut, a.StayDurationDays,
            a.DepositAmountCents, a.InsuranceFeeCents, a.FirstMonthRentCents,
            a.PartnerOrganizationId, a.IsPartnerReferred, a.JurisdictionWarning, a.Source,
            a.GuestCount, a.Message);
}
