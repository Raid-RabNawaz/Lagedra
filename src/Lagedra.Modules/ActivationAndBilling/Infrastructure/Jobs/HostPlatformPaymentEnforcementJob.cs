using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class HostPlatformPaymentEnforcementJob(
    BillingDbContext dbContext,
    IClock clock,
    IPlatformSettingsService settings,
    ILogger<HostPlatformPaymentEnforcementJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;
        var reminderIntervalDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.HostPlatformPaymentReminderIntervalDays, 2, ct).ConfigureAwait(false);
        var suspendAfterDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.HostPlatformPaymentSuspendAfterDays, 14, ct).ConfigureAwait(false);

        var unpaid = await dbContext.DealPaymentConfirmations
            .Where(c => c.Status == PaymentConfirmationStatus.Confirmed
                && c.HostConfirmed
                && !c.HostPaidPlatform)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (unpaid.Count == 0)
        {
            return;
        }

        LogUnpaidFound(logger, unpaid.Count);

        foreach (var confirmation in unpaid)
        {
            if (confirmation.HostShouldBeSuspended(clock, suspendAfterDays))
            {
                await SuspendHostAsync(confirmation, ct).ConfigureAwait(false);
            }
            else if (confirmation.HostNeedsPlatformPaymentReminder(clock, reminderIntervalDays))
            {
                confirmation.MarkHostPlatformReminderSent(clock);
                LogReminderSent(logger, confirmation.DealId);
            }
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task SuspendHostAsync(
        Domain.Aggregates.DealPaymentConfirmation confirmation,
        CancellationToken ct)
    {
        LogSuspending(logger, confirmation.DealId);

        var billingAccount = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.DealId == confirmation.DealId
                && b.Status == BillingAccountStatus.Active, ct)
            .ConfigureAwait(false);

        billingAccount?.Suspend();
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Found {Count} deals where host confirmed tenant payment but has not paid platform")]
    private static partial void LogUnpaidFound(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Sending host platform payment reminder for deal {DealId}")]
    private static partial void LogReminderSent(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Suspending host billing account for deal {DealId}: platform fee not paid")]
    private static partial void LogSuspending(ILogger logger, Guid dealId);
}
