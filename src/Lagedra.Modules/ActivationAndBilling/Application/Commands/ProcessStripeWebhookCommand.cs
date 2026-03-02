using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
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
    ILogger<ProcessStripeWebhookCommandHandler> logger)
    : IRequestHandler<ProcessStripeWebhookCommand, Result>
{
    public async Task<Result> Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await stripeService.HandleWebhookAsync(request.Payload, request.Signature, cancellationToken)
            .ConfigureAwait(false);

        var stripeEvent = Stripe.EventUtility.ParseEvent(request.Payload);

        switch (stripeEvent.Type)
        {
            case Stripe.EventTypes.PaymentIntentSucceeded:
                await HandlePaymentSucceeded(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case Stripe.EventTypes.PaymentIntentPaymentFailed:
                await HandlePaymentFailed(stripeEvent, cancellationToken).ConfigureAwait(false);
                break;

            case Stripe.EventTypes.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeleted(stripeEvent, cancellationToken).ConfigureAwait(false);
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

        var customerId = paymentIntent.CustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            return;
        }

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.StripeCustomerId == customerId, ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        LogPaymentSucceeded(logger, account.DealId, paymentIntent.Amount);
    }

    private async Task HandlePaymentFailed(Stripe.Event stripeEvent, CancellationToken ct)
    {
        if (stripeEvent.Data.Object is not Stripe.PaymentIntent paymentIntent)
        {
            return;
        }

        var customerId = paymentIntent.CustomerId;
        if (string.IsNullOrEmpty(customerId))
        {
            return;
        }

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.StripeCustomerId == customerId, ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return;
        }

        LogPaymentFailed(logger, account.DealId, paymentIntent.Amount);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe payment succeeded for deal {DealId}, amount {AmountCents}")]
    private static partial void LogPaymentSucceeded(ILogger logger, Guid dealId, long? amountCents);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stripe payment failed for deal {DealId}, amount {AmountCents}")]
    private static partial void LogPaymentFailed(ILogger logger, Guid dealId, long? amountCents);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe subscription deleted for deal {DealId}, subscription {SubscriptionId}")]
    private static partial void LogSubscriptionDeleted(ILogger logger, Guid dealId, string subscriptionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Unhandled Stripe event type: {EventType}")]
    private static partial void LogUnhandledEvent(ILogger logger, string eventType);
}
