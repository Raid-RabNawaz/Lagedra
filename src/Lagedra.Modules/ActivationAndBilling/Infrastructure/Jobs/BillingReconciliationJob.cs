using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class BillingReconciliationJob(
    BillingDbContext dbContext,
    ILogger<BillingReconciliationJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var activeAccounts = await dbContext.BillingAccounts
            .AsNoTracking()
            .Include(b => b.Invoices)
            .Where(b => b.Status == BillingAccountStatus.Active)
            .ToListAsync(context.CancellationToken)
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

        LogReconciliationComplete(logger, activeAccounts.Count, issuesFound);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Billing account {AccountId} (Deal {DealId}) has {FailedCount} failed invoices")]
    private static partial void LogFailedInvoicesDetected(ILogger logger, Guid accountId, Guid dealId, int failedCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Billing account {AccountId} (Deal {DealId}) is missing Stripe customer ID")]
    private static partial void LogMissingStripeCustomer(ILogger logger, Guid accountId, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Billing reconciliation complete: {Total} active accounts checked, {Issues} issues found")]
    private static partial void LogReconciliationComplete(ILogger logger, int total, int issues);
}
