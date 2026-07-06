using Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Jobs;

/// <summary>
/// Non-custodial deposit model: Lagedra never holds the deposit, so it cannot
/// auto-refund it. Once a stay ends (billing closed) and the damage-claim
/// window has passed, this job nudges the host to return the deposit directly
/// and the tenant to confirm receipt, so the deal can complete. Money is only
/// moved by the platform through the admin/arbitration force-deposit-return
/// fallback, never here.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class DepositReturnJob(
    BillingDbContext dbContext,
    IMediator mediator,
    IClock clock,
    IPlatformSettingsService settings,
    ILogger<DepositReturnJob> logger) : IJob
{
    private const int ReminderIntervalDays = 7;

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;
        var claimDeadlineDays = (int)await settings
            .GetLongAsync(PlatformSettingKeys.DamageClaimFilingDeadlineDays, 14, ct)
            .ConfigureAwait(false);

        // Only start nudging once the claim window has closed — before that the
        // host may still be assessing damages.
        var cutoff = clock.UtcNow.AddDays(-claimDeadlineDays);

        var closedDeals = await dbContext.BillingAccounts
            .Where(b => b.Status == BillingAccountStatus.Closed
                && b.EndDate != null
                && b.EndDate.Value <= cutoff)
            .Select(b => b.DealId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (closedDeals.Count == 0)
        {
            return;
        }

        // Tracked (no AsNoTracking): we stamp the reminder timestamp and save.
        var openHandshakes = await dbContext.DealPaymentConfirmations
            .Where(c => closedDeals.Contains(c.DealId)
                && c.Status == PaymentConfirmationStatus.Confirmed
                && c.DepositAmountCents > 0
                && c.DepositReturnSettledAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (openHandshakes.Count == 0)
        {
            return;
        }

        var dealIds = openHandshakes.Select(c => c.DealId).ToList();

        var participants = await dbContext.DealApplications
            .AsNoTracking()
            .Where(a => a.DealId != null && dealIds.Contains(a.DealId!.Value))
            .Select(a => new { DealId = a.DealId!.Value, a.LandlordUserId, a.TenantUserId })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var participantMap = participants.ToDictionary(p => p.DealId);

        var reminded = 0;
        foreach (var confirmation in openHandshakes)
        {
            if (!confirmation.DepositReturnReminderDue(clock, ReminderIntervalDays))
            {
                continue;
            }

            if (!participantMap.TryGetValue(confirmation.DealId, out var parties))
            {
                continue;
            }

            if (confirmation.HostConfirmedDepositReturnedAt is null)
            {
                await mediator.Send(new NotifyUserCommand(
                    parties.LandlordUserId, "deposit_return_due",
                    "Return the security deposit",
                    "The stay has ended. Please return the security deposit to your guest "
                    + "directly, then confirm it in the app so the booking can be completed.",
                    new() { ["dealId"] = confirmation.DealId.ToString() },
                    Channels.EmailAndInApp, confirmation.DealId, "Deal"), ct).ConfigureAwait(false);
            }
            else if (confirmation.TenantConfirmedDepositReceivedAt is null)
            {
                await mediator.Send(new NotifyUserCommand(
                    parties.TenantUserId, "deposit_receipt_due",
                    "Confirm your deposit was returned",
                    "Your host marked your security deposit as returned. Please confirm you "
                    + "received it to complete the booking — or raise a dispute if you didn't.",
                    new() { ["dealId"] = confirmation.DealId.ToString() },
                    Channels.EmailAndInApp, confirmation.DealId, "Deal"), ct).ConfigureAwait(false);
            }

            confirmation.MarkDepositReturnReminderSent(clock);
            reminded++;
        }

        if (reminded > 0)
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        LogRemindersSent(logger, reminded, openHandshakes.Count);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "DepositReturnJob: sent {Reminded} deposit-return reminder(s) across {Open} open handshake(s)")]
    private static partial void LogRemindersSent(ILogger logger, int reminded, int open);
}
