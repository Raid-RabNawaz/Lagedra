using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

public sealed record ListMyDealsQuery(
    Guid UserId,
    string? PhaseFilter) : IRequest<Result<IReadOnlyList<DealSummaryDto>>>;

public sealed class ListMyDealsQueryHandler(
    BillingDbContext dbContext,
    IListingProvider listingProvider,
    ITruthSurfaceStatusProvider truthSurfaceStatusProvider)
    : IRequestHandler<ListMyDealsQuery, Result<IReadOnlyList<DealSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<DealSummaryDto>>> Handle(
        ListMyDealsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var applications = await dbContext.DealApplications
            .AsNoTracking()
            .Where(a =>
                (a.TenantUserId == request.UserId || a.LandlordUserId == request.UserId)
                && a.DealId != null)
            .OrderByDescending(a => a.DecidedAt ?? a.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (applications.Count == 0)
        {
            return Result<IReadOnlyList<DealSummaryDto>>.Success(
                Array.Empty<DealSummaryDto>());
        }

        var dealIds = applications.Select(a => a.DealId!.Value).ToList();

        var billingAccounts = await dbContext.BillingAccounts
            .AsNoTracking()
            .Where(b => dealIds.Contains(b.DealId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var paymentConfirmations = await dbContext.DealPaymentConfirmations
            .AsNoTracking()
            .Where(p => dealIds.Contains(p.DealId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var truthSurfaceStatuses = await truthSurfaceStatusProvider
            .GetStatusesForDealsAsync(dealIds, cancellationToken)
            .ConfigureAwait(false);

        var listingIds = applications
            .Select(a => a.ListingId)
            .Distinct()
            .ToList();

        var listingSummaries = await listingProvider
            .GetListingSummariesAsync(listingIds, cancellationToken)
            .ConfigureAwait(false);

        var listingMap = listingSummaries.ToDictionary(l => l.Id);
        var billingMap = billingAccounts.ToDictionary(b => b.DealId);
        var paymentMap = paymentConfirmations.ToDictionary(p => p.DealId);

        var results = new List<DealSummaryDto>(applications.Count);

        foreach (var app in applications)
        {
            var dealId = app.DealId!.Value;
            listingMap.TryGetValue(app.ListingId, out var listing);
            billingMap.TryGetValue(dealId, out var billing);
            paymentMap.TryGetValue(dealId, out var payment);
            truthSurfaceStatuses.TryGetValue(dealId, out var truthSurface);

            var phase = ComputeDealPhase(app, billing, payment, truthSurface);

            if (!MatchesFilter(phase, request.PhaseFilter))
            {
                continue;
            }

            long? totalAmount = app.FirstMonthRentCents.HasValue
                ? (app.FirstMonthRentCents.Value
                   + (app.DepositAmountCents ?? 0)
                   + (app.InsuranceFeeCents ?? 0))
                : null;

            results.Add(new DealSummaryDto(
                dealId,
                app.Id,
                app.ListingId,
                listing?.Title ?? "Listing",
                listing?.CoverPhotoUri,
                listing?.City,
                app.TenantUserId,
                app.LandlordUserId,
                app.Status,
                phase,
                app.RequestedCheckIn,
                app.RequestedCheckOut,
                app.StayDurationDays,
                app.FirstMonthRentCents,
                app.DepositAmountCents,
                totalAmount,
                billing?.Status,
                payment?.Status,
                app.SubmittedAt));
        }

        return Result<IReadOnlyList<DealSummaryDto>>.Success(results);
    }

    private static string ComputeDealPhase(
        Domain.Aggregates.DealApplication app,
        Domain.Aggregates.BillingAccount? billing,
        Domain.Aggregates.DealPaymentConfirmation? payment,
        TruthSurfaceSnapshotInfo? truthSurface)
    {
        if (app.Status == DealApplicationStatus.Cancelled)
        {
            return "Cancelled";
        }

        if (billing is not null)
        {
            return billing.Status switch
            {
                BillingAccountStatus.Active => "Active",
                BillingAccountStatus.Suspended => "Active",
                BillingAccountStatus.Closed => "Closed",
                _ => DerivePreActivationPhase(payment, truthSurface),
            };
        }

        return DerivePreActivationPhase(payment, truthSurface);
    }

    private static string DerivePreActivationPhase(
        Domain.Aggregates.DealPaymentConfirmation? payment,
        TruthSurfaceSnapshotInfo? truthSurface)
    {
        if (payment is not null)
        {
            return payment.Status switch
            {
                PaymentConfirmationStatus.Pending => "AwaitingPayment",
                PaymentConfirmationStatus.Confirmed => "Active",
                PaymentConfirmationStatus.Disputed => "AwaitingPayment",
                PaymentConfirmationStatus.Rejected => "Cancelled",
                PaymentConfirmationStatus.Cancelled => "Cancelled",
                _ => "Inquiry",
            };
        }

        if (truthSurface is not null)
        {
            return truthSurface.IsSealed ? "AwaitingPayment" : "TruthSurface";
        }

        return "Inquiry";
    }

    private static bool MatchesFilter(string phase, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)
            || string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(filter, "active", StringComparison.OrdinalIgnoreCase))
        {
            return phase is "Inquiry" or "TruthSurface" or "AwaitingPayment" or "Checkout" or "Active";
        }

        if (string.Equals(filter, "past", StringComparison.OrdinalIgnoreCase))
        {
            return phase is "Closed" or "Cancelled";
        }

        return true;
    }
}
