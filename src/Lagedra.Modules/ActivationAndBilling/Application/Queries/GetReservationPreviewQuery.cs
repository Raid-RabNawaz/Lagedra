using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

/// <summary>
/// Computes the reservation price breakdown for a tenant + listing + dates
/// without creating anything. Backs the apply dialog so the tenant sees the
/// exact deposit (for their verification tier), fees, and total before they
/// consent + pay.
/// </summary>
public sealed record GetReservationPreviewQuery(
    Guid ListingId,
    Guid TenantUserId,
    DateOnly CheckIn,
    DateOnly CheckOut) : IRequest<Result<ReservationPreviewDto>>;

public sealed class GetReservationPreviewQueryHandler(
    IListingProvider listingProvider,
    IReservationPricingService reservationPricingService)
    : IRequestHandler<GetReservationPreviewQuery, Result<ReservationPreviewDto>>
{
    private static readonly Error ListingNotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error InvalidDates = new("Dates.Invalid", "Check-out must be after check-in.");

    public async Task<Result<ReservationPreviewDto>> Handle(
        GetReservationPreviewQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CheckOut <= request.CheckIn)
        {
            return Result<ReservationPreviewDto>.Failure(InvalidDates);
        }

        var listing = await listingProvider
            .GetListingDetailsAsync(request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<ReservationPreviewDto>.Failure(ListingNotFound);
        }

        var duration = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        var pricing = await reservationPricingService
            .ComputeAsync(listing, request.TenantUserId, duration, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return Result<ReservationPreviewDto>.Success(new ReservationPreviewDto(
            request.ListingId,
            pricing.Tier,
            pricing.DepositCents,
            pricing.DepositReason,
            pricing.FirstMonthRentCents,
            pricing.InsuranceFeeCents,
            pricing.ServiceFeeCents,
            pricing.MonthlyProtocolFeeCents,
            pricing.TotalPayableCents,
            duration,
            pricing.IsNegotiatedOffer,
            pricing.NegotiatedOfferId));
    }
}
