using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

/// <summary>
/// Builds the caller's consolidated host billing statement: the recurring
/// monthly platform fee they owe per active booking, plus the full history of
/// deductions Stripe has taken across all their billing accounts.
/// </summary>
public sealed record GetHostBillingStatementQuery(Guid HostUserId)
    : IRequest<Result<HostBillingStatementDto>>;

public sealed class GetHostBillingStatementQueryHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider,
    IPlatformSettingsService settings)
    : IRequestHandler<GetHostBillingStatementQuery, Result<HostBillingStatementDto>>
{
    public async Task<Result<HostBillingStatementDto>> Handle(
        GetHostBillingStatementQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currentMonthlyFeeCents = await ResolveProtocolFeeAsync(cancellationToken)
            .ConfigureAwait(false);

        var accounts = await dbContext.BillingAccounts
            .AsNoTracking()
            .Include(b => b.Invoices)
            .Where(b => b.LandlordUserId == request.HostUserId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (accounts.Count == 0)
        {
            return Result<HostBillingStatementDto>.Success(new HostBillingStatementDto(
                ActiveBookingCount: 0,
                CurrentMonthlyFeeCents: currentMonthlyFeeCents,
                ProjectedMonthlyTotalCents: 0,
                TotalPaidToDateCents: 0,
                TotalOutstandingCents: 0,
                Invoices: Array.Empty<InvoiceDto>()));
        }

        var dealListingTitles = await ResolveDealListingTitlesAsync(
            accounts.Select(a => a.DealId).Distinct().ToList(),
            cancellationToken).ConfigureAwait(false);

        var invoices = accounts
            .SelectMany(a => a.Invoices.Select(i => new InvoiceDto(
                i.Id,
                a.DealId,
                dealListingTitles.GetValueOrDefault(a.DealId),
                i.PeriodStart,
                i.PeriodEnd,
                i.AmountCents,
                i.Status,
                i.CreatedAt)))
            .OrderByDescending(i => i.PeriodStart)
            .ThenByDescending(i => i.CreatedAt)
            .ToList();

        var activeBookingCount = accounts.Count(a => a.Status == BillingAccountStatus.Active);
        var totalPaid = invoices
            .Where(i => i.Status == InvoiceStatus.Paid)
            .Sum(i => (long)i.AmountCents);
        var totalOutstanding = invoices
            .Where(i => i.Status is InvoiceStatus.Pending or InvoiceStatus.Failed)
            .Sum(i => (long)i.AmountCents);

        return Result<HostBillingStatementDto>.Success(new HostBillingStatementDto(
            ActiveBookingCount: activeBookingCount,
            CurrentMonthlyFeeCents: currentMonthlyFeeCents,
            ProjectedMonthlyTotalCents: activeBookingCount * currentMonthlyFeeCents,
            TotalPaidToDateCents: totalPaid,
            TotalOutstandingCents: totalOutstanding,
            Invoices: invoices));
    }

    /// <summary>
    /// A <see cref="Domain.Aggregates.BillingAccount"/> only carries a DealId,
    /// so resolve each deal's listing title by hopping through the deal's
    /// application row (deal → listing) and batch-loading titles.
    /// </summary>
    private async Task<Dictionary<Guid, string?>> ResolveDealListingTitlesAsync(
        List<Guid> dealIds,
        CancellationToken cancellationToken)
    {
        var dealListingPairs = await dbContext.DealApplications
            .AsNoTracking()
            .Where(a => a.DealId != null && dealIds.Contains(a.DealId.Value))
            .Select(a => new { DealId = a.DealId!.Value, a.ListingId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dealToListing = dealListingPairs
            .GroupBy(p => p.DealId)
            .ToDictionary(g => g.Key, g => g.First().ListingId);

        var listingSummaries = await listingProvider
            .GetListingSummariesAsync(dealToListing.Values.Distinct().ToList(), cancellationToken)
            .ConfigureAwait(false);
        var listingTitles = listingSummaries.ToDictionary(l => l.Id, l => l.Title);

        return dealToListing.ToDictionary(
            kvp => kvp.Key,
            kvp => listingTitles.GetValueOrDefault(kvp.Value));
    }

    private async Task<long> ResolveProtocolFeeAsync(CancellationToken cancellationToken)
    {
        var monthlyFee = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeeMonthly, 7900, cancellationToken)
            .ConfigureAwait(false);
        var pilotDiscount = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeePilotDiscount, 3900, cancellationToken)
            .ConfigureAwait(false);
        var isPilot = await settings
            .GetBoolAsync(PlatformSettingKeys.ProtocolFeePilotActive, false, cancellationToken)
            .ConfigureAwait(false);

        return isPilot ? monthlyFee - pilotDiscount : monthlyFee;
    }
}
