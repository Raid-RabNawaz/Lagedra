using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class BillingReconciliationJob(
    BillingDbContext dbContext,
    IStripeService stripeService,
    IPlatformSettingsService settings,
    ILogger<BillingReconciliationJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;

        var activeAccounts = await dbContext.BillingAccounts
            .AsNoTracking()
            .Include(b => b.Invoices)
            .Where(b => b.Status == BillingAccountStatus.Active)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var issuesFound = 0;

        foreach (var account in activeAccounts)
        {
            var failedInvoices = account.Invoices
                .Count(i => i.Status == InvoiceStatus.Failed);

            if (failedInvoices > 0)
            {
                issuesFound++;
                LogFailedInvoicesDetected(logger, account.Id, account.DealId, failedInvoices);
            }

            if (account.StripeCustomerId is null)
            {
                issuesFound++;
                LogMissingStripeCustomer(logger, account.Id, account.DealId);
            }
        }

        await ReconcileProtocolFeeAsync(ct).ConfigureAwait(false);

        LogReconciliationComplete(logger, activeAccounts.Count, issuesFound);
    }

    /// <summary>
    /// Guards against the configured protocol fee (what hosts are *shown* across
    /// the dashboard, payout page, approval surfaces and quotes) drifting away
    /// from the Stripe subscription price they are *actually* billed. The two are
    /// set independently — the display value lives in platform settings while the
    /// charge is tied to the Stripe Price — so a mismatch means hosts see the
    /// wrong number. We only detect and log here; correcting either side is a
    /// deliberate operator action.
    /// </summary>
    private async Task ReconcileProtocolFeeAsync(CancellationToken ct)
    {
        var priceId = await settings
            .GetStringAsync(PlatformSettingKeys.StripePlatformFeePriceId, ct)
            .ConfigureAwait(false);

        if (string.IsNullOrEmpty(priceId))
        {
            LogPriceIdNotConfigured(logger);
            return;
        }

        var configuredFeeCents = await ResolveConfiguredProtocolFeeAsync(ct).ConfigureAwait(false);

        long? stripeAmountCents;
        try
        {
            stripeAmountCents = await stripeService
                .GetPriceAmountCentsAsync(priceId, ct)
                .ConfigureAwait(false);
        }
        catch (Stripe.StripeException ex)
        {
            LogPriceLookupFailed(logger, priceId, ex.Message);
            return;
        }

        if (stripeAmountCents is null)
        {
            LogPriceHasNoUnitAmount(logger, priceId);
            return;
        }

        if (stripeAmountCents.Value != configuredFeeCents)
        {
            LogProtocolFeeDrift(logger, configuredFeeCents, stripeAmountCents.Value, priceId);
        }
        else
        {
            LogProtocolFeeReconciled(logger, configuredFeeCents);
        }
    }

    private async Task<long> ResolveConfiguredProtocolFeeAsync(CancellationToken ct)
    {
        var monthlyFee = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeeMonthly, 7900, ct)
            .ConfigureAwait(false);
        var pilotDiscount = await settings
            .GetLongAsync(PlatformSettingKeys.ProtocolFeePilotDiscount, 3900, ct)
            .ConfigureAwait(false);
        var isPilot = await settings
            .GetBoolAsync(PlatformSettingKeys.ProtocolFeePilotActive, false, ct)
            .ConfigureAwait(false);

        return isPilot ? monthlyFee - pilotDiscount : monthlyFee;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Billing account {AccountId} (Deal {DealId}) has {FailedCount} failed invoices")]
    private static partial void LogFailedInvoicesDetected(ILogger logger, Guid accountId, Guid dealId, int failedCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Billing account {AccountId} (Deal {DealId}) is missing Stripe customer ID")]
    private static partial void LogMissingStripeCustomer(ILogger logger, Guid accountId, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Billing reconciliation complete: {Total} active accounts checked, {Issues} issues found")]
    private static partial void LogReconciliationComplete(ILogger logger, int total, int issues);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Protocol fee drift: platform setting shows {ConfiguredCents}c but Stripe price {PriceId} charges {StripeCents}c. Hosts are seeing the wrong monthly fee — align the ProtocolFee setting or the Stripe price.")]
    private static partial void LogProtocolFeeDrift(ILogger logger, long configuredCents, long stripeCents, string priceId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Protocol fee reconciled: configured and Stripe price agree at {ConfiguredCents}c")]
    private static partial void LogProtocolFeeReconciled(ILogger logger, long configuredCents);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Protocol fee reconciliation skipped: StripePlatformFeePriceId is not configured")]
    private static partial void LogPriceIdNotConfigured(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Protocol fee reconciliation skipped: could not read Stripe price {PriceId}: {Reason}")]
    private static partial void LogPriceLookupFailed(ILogger logger, string priceId, string reason);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Protocol fee reconciliation skipped: Stripe price {PriceId} has no fixed unit amount (tiered pricing?)")]
    private static partial void LogPriceHasNoUnitAmount(ILogger logger, string priceId);
}
