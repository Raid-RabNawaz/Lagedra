using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
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
/// — there is no public endpoint that maps directly to this command.
/// </summary>
public sealed record SubmitPartnerDirectApplicationCommand(
    Guid ListingId,
    Guid TenantUserId,
    Guid PartnerOrganizationId,
    DateOnly RequestedCheckIn,
    DateOnly RequestedCheckOut,
    ApplicationPayerType PayerType = ApplicationPayerType.Tenant,
    Guid? PayerUserId = null,
    string? StripePaymentMethodId = null) : IRequest<Result<DealApplicationDto>>;

public sealed class SubmitPartnerDirectApplicationCommandHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider,
    IReservationPricingService reservationPricingService)
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
    private static readonly Error PartnerPaymentRequired =
        new("Application.PartnerPaymentRequired",
            "A payment method is required when the partner organization pays.");

    public async Task<Result<DealApplicationDto>> Handle(
        SubmitPartnerDirectApplicationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.PayerType == ApplicationPayerType.PartnerOrganization
            && string.IsNullOrWhiteSpace(request.StripePaymentMethodId))
        {
            return Result<DealApplicationDto>.Failure(PartnerPaymentRequired);
        }

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

        // Partner direct reservations always price at the partner-guaranteed
        // tier (the partner is vouching for the guest), snapshotted up-front.
        var pricing = await reservationPricingService
            .ComputeAsync(
                listing,
                request.TenantUserId,
                duration,
                forcedTier: TenantVerificationTier.PartnerGuaranteed,
                forcedPartnerOrganizationId: request.PartnerOrganizationId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var application = DealApplication.Submit(
            request.ListingId,
            request.TenantUserId,
            listing.LandlordUserId,
            request.RequestedCheckIn,
            request.RequestedCheckOut,
            partnerOrganizationId: request.PartnerOrganizationId,
            isPartnerReferred: true,
            source: DealApplicationSource.PartnerDirectReservation,
            stripePaymentMethodId: request.StripePaymentMethodId,
            depositSnapshot: pricing.ToSnapshot(),
            payerType: request.PayerType,
            payerUserId: request.PayerUserId);

        OwnerTenancyConsent.ApplyIfRequired(application, listing);

        dbContext.DealApplications.Add(application);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DealApplicationDto>.Success(DealApplicationDtoMapper.ToDto(application));
    }
}
