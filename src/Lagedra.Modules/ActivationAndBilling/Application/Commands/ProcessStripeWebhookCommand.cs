using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ProcessStripeWebhookCommand(
    string Payload,
    string Signature) : IRequest<Result>;

public sealed partial class ProcessStripeWebhookCommandHandler(
    BillingDbContext dbContext,
    IStripeService stripeService,
    IMediator mediator,
    IEventBus eventBus,
    IClock clock,
    ILogger<ProcessStripeWebhookCommandHandler> logger)
    : IRequestHandler<ProcessStripeWebhookCommand, Result>
{
    public async Task<Result> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stripeEvent = await stripeService
            .VerifyWebhookAsync(request.Payload, request.Signature)
            .ConfigureAwait(false);

        switch (stripeEvent.Type)
        {
            case Stripe.EventTypes.PaymentIntentSucceeded:
                await HandlePaymentSucceeded(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case Stripe.EventTypes.PaymentIntentPaymentFailed:
                await HandlePaymentFailed(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case Stripe.EventTypes.PaymentIntentCanceled:
                await HandlePaymentCanceled(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case Stripe.EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeleted(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case Stripe.EventTypes.InvoicePaymentFailed:
                await HandleInvoicePaymentFailed(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case Stripe.EventTypes.InvoicePaid:
                await HandleInvoicePaid(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case Stripe.EventTypes.ChargeRefunded:
                await HandleChargeRefunded(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case Stripe.EventTypes.ChargeDisputeCreated:
                await HandleChargeDisputeCreated(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case "account.updated":
                await HandleAccountUpdated(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            default:
                LogUnhandledEvent(logger, stripeEvent.Type);
                break;
        }

        return Result.Success();
    }

    private async Task HandlePaymentSucceeded(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.PaymentIntent paymentIntent)
        {
            return;
        }

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.StripePaymentIntentId == paymentIntent.Id, ct)
            .ConfigureAwait(false);

        if (confirmation is not null)
        {
            confirmation.ConfirmByStripe(clock);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            LogActivationPaymentSucceeded(logger, confirmation.DealId, paymentIntent.Amount);

            await mediator.Send(new ActivateDealCommand(confirmation.DealId), ct).ConfigureAwait(false);
            return;
        }

        var customerId = paymentIntent.CustomerId;
        if (!string.IsNullOrEmpty(customerId))
        {
            var account = await dbContext.BillingAccounts
                .FirstOrDefaultAsync(b => b.StripeCustomerId == customerId, ct)
                .ConfigureAwait(false);

            if (account is not null)
            {
                LogPaymentSucceeded(logger, account.DealId, paymentIntent.Amount);
            }
        }
    }

    private async Task HandlePaymentFailed(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.PaymentIntent paymentIntent)
        {
            return;
        }

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.StripePaymentIntentId == paymentIntent.Id, ct)
            .ConfigureAwait(false);

        if (confirmation is not null)
        {
            confirmation.FailByStripe(clock);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            LogActivationPaymentFailed(logger, confirmation.DealId, paymentIntent.Amount);
            return;
        }

        var customerId = paymentIntent.CustomerId;
        if (!string.IsNullOrEmpty(customerId))
        {
            var account = await dbContext.BillingAccounts
                .FirstOrDefaultAsync(b => b.StripeCustomerId == customerId, ct)
                .ConfigureAwait(false);

            if (account is not null)
            {
                LogPaymentFailed(logger, account.DealId, paymentIntent.Amount);
            }
        }
    }

    private async Task HandlePaymentCanceled(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.PaymentIntent paymentIntent)
        {
            return;
        }

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.StripePaymentIntentId == paymentIntent.Id, ct)
            .ConfigureAwait(false);

        if (confirmation is not null && confirmation.Status == PaymentConfirmationStatus.Pending)
        {
            confirmation.Cancel("Payment intent was canceled by the payment provider.", clock);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            LogPaymentIntentCanceled(logger, confirmation.DealId, paymentIntent.Id);
        }
    }

    private async Task HandleSubscriptionDeleted(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.Subscription subscription)
        {
            return;
        }

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.StripeSubscriptionId == subscription.Id, ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        if (account.Status is BillingAccountStatus.Active or BillingAccountStatus.Inactive)
        {
            account.Close();
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            LogSubscriptionDeleted(logger, account.DealId, subscription.Id);
        }
    }

    private async Task HandleInvoicePaid(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.Invoice invoice || string.IsNullOrEmpty(invoice.SubscriptionId))
        {
            return;
        }

        var account = await dbContext.BillingAccounts
            .Include(b => b.Invoices)
            .FirstOrDefaultAsync(b => b.StripeSubscriptionId == invoice.SubscriptionId, ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        var existingInvoice = account.Invoices
            .FirstOrDefault(i => i.StripeInvoiceId == invoice.Id);

        if (existingInvoice is not null)
        {
            if (existingInvoice.Status != Domain.Enums.InvoiceStatus.Paid)
            {
                existingInvoice.MarkPaid();
            }
        }
        else
        {
            var newInvoice = Domain.Entities.Invoice.Create(
                account.Id,
                invoice.PeriodStart,
                invoice.PeriodEnd,
                (int)invoice.AmountPaid,
                stripeInvoiceId: invoice.Id);
            newInvoice.MarkPaid();
            dbContext.Invoices.Add(newInvoice);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        LogInvoicePaid(logger, account.DealId, invoice.Id, invoice.AmountPaid);
    }

    private async Task HandleInvoicePaymentFailed(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.Invoice invoice || string.IsNullOrEmpty(invoice.SubscriptionId))
        {
            return;
        }

        var account = await dbContext.BillingAccounts
            .Include(b => b.Invoices)
            .FirstOrDefaultAsync(b => b.StripeSubscriptionId == invoice.SubscriptionId, ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        var existingInvoice = account.Invoices
            .FirstOrDefault(i => i.StripeInvoiceId == invoice.Id);

        if (existingInvoice is not null)
        {
            if (existingInvoice.Status == Domain.Enums.InvoiceStatus.Pending)
            {
                existingInvoice.MarkFailed();
            }
        }
        else
        {
            var newInvoice = Domain.Entities.Invoice.Create(
                account.Id,
                invoice.PeriodStart,
                invoice.PeriodEnd,
                (int)invoice.AmountDue,
                stripeInvoiceId: invoice.Id);
            newInvoice.MarkFailed();
            dbContext.Invoices.Add(newInvoice);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        LogInvoicePaymentFailed(logger, account.DealId, invoice.Id, invoice.AmountDue);
    }

    private async Task HandleChargeRefunded(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.Charge charge || string.IsNullOrEmpty(charge.PaymentIntentId))
        {
            return;
        }

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.StripePaymentIntentId == charge.PaymentIntentId, ct)
            .ConfigureAwait(false);

        if (confirmation is not null)
        {
            confirmation.SetStripePaymentIntent(charge.PaymentIntentId, "refunded", clock);
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        LogChargeRefunded(logger, charge.Id, charge.PaymentIntentId, charge.AmountRefunded);
    }

    private async Task HandleChargeDisputeCreated(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.Dispute dispute)
        {
            return;
        }

        LogChargeDisputeCreated(logger, dispute.Id, dispute.ChargeId, dispute.Amount);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task HandleAccountUpdated(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.Account account)
        {
            return;
        }

        await eventBus.Publish(
            new StripeAccountUpdatedEvent(account.Id, account.ChargesEnabled, account.PayoutsEnabled), ct)
            .ConfigureAwait(false);

        LogAccountUpdated(logger, account.Id, account.ChargesEnabled, account.PayoutsEnabled);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Activation payment succeeded for deal {DealId}, amount {AmountCents} — auto-activating")]
    private static partial void LogActivationPaymentSucceeded(ILogger logger, Guid dealId, long? amountCents);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Activation payment failed for deal {DealId}, amount {AmountCents}")]
    private static partial void LogActivationPaymentFailed(ILogger logger, Guid dealId, long? amountCents);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe payment succeeded for deal {DealId}, amount {AmountCents}")]
    private static partial void LogPaymentSucceeded(ILogger logger, Guid dealId, long? amountCents);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe payment failed for deal {DealId}, amount {AmountCents}")]
    private static partial void LogPaymentFailed(ILogger logger, Guid dealId, long? amountCents);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe PaymentIntent canceled for deal {DealId}, PI {PaymentIntentId}")]
    private static partial void LogPaymentIntentCanceled(ILogger logger, Guid dealId, string paymentIntentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe subscription deleted for deal {DealId}, subscription {SubscriptionId}")]
    private static partial void LogSubscriptionDeleted(ILogger logger, Guid dealId, string subscriptionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe invoice paid for deal {DealId}, invoice {InvoiceId}, amount {AmountPaid}")]
    private static partial void LogInvoicePaid(ILogger logger, Guid dealId, string invoiceId, long amountPaid);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe invoice payment failed for deal {DealId}, invoice {InvoiceId}, amount {AmountDue}")]
    private static partial void LogInvoicePaymentFailed(ILogger logger, Guid dealId, string invoiceId, long amountDue);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe charge refunded: {ChargeId}, PI {PaymentIntentId}, refund amount {AmountRefunded}")]
    private static partial void LogChargeRefunded(ILogger logger, string chargeId, string? paymentIntentId, long amountRefunded);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe charge dispute created: {DisputeId}, charge {ChargeId}, amount {Amount}")]
    private static partial void LogChargeDisputeCreated(ILogger logger, string disputeId, string chargeId, long amount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe connected account updated: {AccountId}, charges={ChargesEnabled}, payouts={PayoutsEnabled}")]
    private static partial void LogAccountUpdated(ILogger logger, string accountId, bool chargesEnabled, bool payoutsEnabled);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Unhandled Stripe event type: {EventType}")]
    private static partial void LogUnhandledEvent(ILogger logger, string eventType);
}
