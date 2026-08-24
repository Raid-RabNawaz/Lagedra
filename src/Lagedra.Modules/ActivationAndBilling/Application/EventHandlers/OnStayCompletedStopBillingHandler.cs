using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

/// <summary>
/// Ends the host's monthly protocol-fee subscription when the stay fully
/// completes (deposit-return handshake settled, or zero-deposit move-out).
/// Without this, Stripe kept invoicing on the activation anniversary until
/// someone remembered to press "Stop billing".
///
/// Uses <c>BillingAccount.Complete()</c> — not <c>Close()</c> — because Close
/// raises BillingStoppedEvent, which the compliance module records as a
/// PaymentDefault signal; a deal that finished its handshake normally must
/// produce the positive DealCompleted signal instead.
/// </summary>
public sealed partial class OnStayCompletedStopBillingHandler(
    BillingDbContext dbContext,
    IStripeService stripeService,
    ILogger<OnStayCompletedStopBillingHandler> logger)
    : IDomainEventHandler<StayCompletedEvent>
{
    public async Task Handle(StayCompletedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.DealId == domainEvent.DealId, ct)
            .ConfigureAwait(false);

        // Nothing to stop: never activated, or already stopped/closed
        // (e.g. the host pressed "Stop billing" manually first).
        if (account is null
            || account.Status is not (BillingAccountStatus.Active or BillingAccountStatus.Suspended))
        {
            return;
        }

        if (!string.IsNullOrEmpty(account.StripeSubscriptionId))
        {
            try
            {
                await stripeService.CancelSubscriptionAsync(account.StripeSubscriptionId, ct)
                    .ConfigureAwait(false);
            }
            catch (Stripe.StripeException ex)
            {
                // Best-effort, same as the manual stop-billing path: the local
                // account still completes so the deal state is right, and the
                // reconciliation job surfaces any orphaned subscription.
                LogStripeCancelFailed(logger, account.DealId, account.StripeSubscriptionId, ex);
            }
            catch (HttpRequestException ex)
            {
                LogStripeCancelFailed(logger, account.DealId, account.StripeSubscriptionId, ex);
            }
        }

        account.Complete();

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        LogBillingCompleted(logger, account.DealId, account.StripeSubscriptionId);
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to cancel Stripe subscription for completed deal {DealId}, subscription {SubscriptionId}")]
    private static partial void LogStripeCancelFailed(ILogger logger, Guid dealId, string subscriptionId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Stay completed — protocol fee billing ended for deal {DealId} (subscription {SubscriptionId})")]
    private static partial void LogBillingCompleted(ILogger logger, Guid dealId, string? subscriptionId);
}
