using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class PaymentConfirmationTimeoutJob(
    BillingDbContext dbContext,
    IClock clock,
    IPlatformSettingsService settings,
    ILogger<PaymentConfirmationTimeoutJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;
        var reminderDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.PaymentReminderAfterDays, 4, ct).ConfigureAwait(false);
        var autoCancelDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.PaymentAutoCancelAfterDays, 7, ct).ConfigureAwait(false);

        var pending = await dbContext.DealPaymentConfirmations
            .Where(c => c.Status == PaymentConfirmationStatus.Pending)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (pending.Count == 0)
        {
            return;
        }

        foreach (var confirmation in pending)
        {
            if (confirmation.ShouldAutoCancel(clock, autoCancelDays))
            {
                await AutoCancelAsync(confirmation, ct).ConfigureAwait(false);
            }
            else if (confirmation.NeedsReminder(clock, reminderDays))
            {
                await SendReminderAsync(confirmation, ct).ConfigureAwait(false);
            }
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task AutoCancelAsync(
        Domain.Aggregates.DealPaymentConfirmation confirmation,
        CancellationToken ct)
    {
        LogAutoCancel(logger, confirmation.DealId);

        confirmation.Cancel("Auto-cancelled: tenant payment not received within deadline", clock);

        var application = await dbContext.DealApplications
            .FirstOrDefaultAsync(a => a.DealId == confirmation.DealId, ct)
            .ConfigureAwait(false);

        application?.Cancel(
            cancelledByUserId: Guid.Empty,
            reason: "Auto-cancelled: tenant payment not received within deadline",
            isAutoCancel: true,
            refundAmountCents: 0,
            insuranceRefundCents: 0);
    }

    private Task SendReminderAsync(
        Domain.Aggregates.DealPaymentConfirmation confirmation,
        CancellationToken ct)
    {
        _ = ct;
        LogReminderSent(logger, confirmation.DealId);
        confirmation.MarkReminderSent(clock);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Auto-cancelling deal {DealId}: tenant payment not received within deadline")]
    private static partial void LogAutoCancel(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Sending payment reminder for deal {DealId}")]
    private static partial void LogReminderSent(ILogger logger, Guid dealId);
}
