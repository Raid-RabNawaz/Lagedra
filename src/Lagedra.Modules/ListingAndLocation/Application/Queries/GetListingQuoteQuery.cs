using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

/// <summary>
/// Phase 16 booking pre-flight quote. Composes the four cost lines a guest
/// sees on the Listing Detail page before clicking Apply / Book:
///
/// <list type="bullet">
///   <item><description>First-month rent (from <c>Listing.MonthlyRentCents</c>).</description></item>
///   <item><description>Security deposit — falls back in this order: explicit
///     <c>DefaultDepositCents</c>, midpoint of the suggested band,
///     <c>MaxDepositCents</c>.</description></item>
///   <item><description>Insurance fee for the requested stay length, computed
///     by <see cref="IInsuranceFeeCalculator"/>.</description></item>
///   <item><description>Monthly protocol fee, charged separately to the host —
///     surfaced for transparency only.</description></item>
/// </list>
///
/// <see cref="QuoteDto.TotalCents"/> sums only the tenant-payable lines
/// (rent + deposit + insurance) so the figure matches what Stripe will
/// charge at checkout. The protocol fee is disclosed as a separate line.
/// </summary>
public sealed record GetListingQuoteQuery(
    Guid ListingId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    Guid? RequesterUserId = null,
    bool RequesterIsPlatformAdmin = false)
    : IRequest<Result<QuoteDto>>;

public sealed class GetListingQuoteQueryHandler(
    ListingsDbContext dbContext,
    IInsuranceFeeCalculator insuranceFeeCalculator,
    IPlatformSettingsService platformSettings)
    : IRequestHandler<GetListingQuoteQuery, Result<QuoteDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error InvalidDates = new(
        "Listing.Quote.InvalidDates",
        "Check-out must be strictly after check-in.");
    private static readonly Error StayBelowMin = new(
        "Listing.Quote.StayBelowMin",
        "Requested stay is shorter than this listing's minimum.");
    private static readonly Error StayAboveMax = new(
        "Listing.Quote.StayAboveMax",
        "Requested stay is longer than this listing's maximum.");

    public async Task<Result<QuoteDto>> Handle(
        GetListingQuoteQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.CheckOut <= request.CheckIn)
        {
            return Result<QuoteDto>.Failure(InvalidDates);
        }

        var listing = await dbContext.Listings
            .AsNoTracking()
            .Where(l => l.Id == request.ListingId)
            .Select(l => new
            {
                l.LandlordUserId,
                l.Status,
                l.MonthlyRentCents,
                l.MaxDepositCents,
                l.DefaultDepositCents,
                l.SuggestedDepositLowCents,
                l.SuggestedDepositHighCents,
                MinStayDays = l.StayRange != null ? (int?)l.StayRange.MinDays : null,
                MaxStayDays = l.StayRange != null ? (int?)l.StayRange.MaxDays : null,
                l.InsuranceRequired,
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<QuoteDto>.Failure(NotFound);
        }

        var isPubliclyVisible =
            listing.Status == ListingStatus.Published ||
            listing.Status == ListingStatus.Activated;
        var isOwner = request.RequesterUserId is Guid uid && uid == listing.LandlordUserId;
        if (!isPubliclyVisible && !isOwner && !request.RequesterIsPlatformAdmin)
        {
            return Result<QuoteDto>.Failure(NotFound);
        }

        var stayDays = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        if (listing.MinStayDays is { } min && stayDays < min)
        {
            return Result<QuoteDto>.Failure(StayBelowMin);
        }

        if (listing.MaxStayDays is { } max && stayDays > max)
        {
            return Result<QuoteDto>.Failure(StayAboveMax);
        }

        var depositCents = ComputeDeposit(
            listing.DefaultDepositCents,
            listing.SuggestedDepositLowCents,
            listing.SuggestedDepositHighCents,
            listing.MaxDepositCents);

        var insuranceQuote = listing.InsuranceRequired
            ? await insuranceFeeCalculator
                .CalculateFeeAsync(listing.MonthlyRentCents, stayDays, cancellationToken)
                .ConfigureAwait(false)
            : new InsuranceFeeQuote(0, "None", null);

        var protocolFeeCents = await ResolveProtocolFeeAsync(cancellationToken).ConfigureAwait(false);

        var serviceFeeCents = await ResolveServiceFeeAsync(
            listing.MonthlyRentCents, cancellationToken).ConfigureAwait(false);

        var totalCents =
            listing.MonthlyRentCents + depositCents + insuranceQuote.FeeCents + serviceFeeCents;

        var dto = new QuoteDto(
            request.CheckIn,
            request.CheckOut,
            stayDays,
            listing.MonthlyRentCents,
            depositCents,
            insuranceQuote.FeeCents,
            protocolFeeCents,
            serviceFeeCents,
            totalCents,
            "USD");

        return Result<QuoteDto>.Success(dto);
    }

    private static long ComputeDeposit(
        long? defaultDepositCents,
        long? suggestedLow,
        long? suggestedHigh,
        long maxDepositCents)
    {
        if (defaultDepositCents.HasValue)
        {
            return defaultDepositCents.Value;
        }

        if (suggestedLow.HasValue && suggestedHigh.HasValue)
        {
            return (suggestedLow.Value + suggestedHigh.Value) / 2;
        }

        return maxDepositCents;
    }

    private async Task<long> ResolveProtocolFeeAsync(CancellationToken cancellationToken)
    {
        var monthlyFee = await platformSettings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeeMonthly, 7900, cancellationToken)
            .ConfigureAwait(false);
        var pilotDiscount = await platformSettings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeePilotDiscount, 3900, cancellationToken)
            .ConfigureAwait(false);
        var isPilot = await platformSettings
            .GetBoolAsync(PlatformSettingKeys.ProtocolFeePilotActive, false, cancellationToken)
            .ConfigureAwait(false);

        return isPilot ? monthlyFee - pilotDiscount : monthlyFee;
    }

    private async Task<long> ResolveServiceFeeAsync(long rentBaseCents, CancellationToken cancellationToken)
    {
        var useFlat = await platformSettings
            .GetBoolAsync(PlatformSettingKeys.TenantServiceFeeUseFlat, false, cancellationToken)
            .ConfigureAwait(false);
        var flatCents = await platformSettings
            .GetLongAsync(PlatformSettingKeys.TenantServiceFeeFlatCents, 0, cancellationToken)
            .ConfigureAwait(false);
        var bps = await platformSettings
            .GetLongAsync(PlatformSettingKeys.TenantServiceFeeBps, 0, cancellationToken)
            .ConfigureAwait(false);

        return TenantServiceFee.Compute(rentBaseCents, useFlat, flatCents, bps);
    }
}
