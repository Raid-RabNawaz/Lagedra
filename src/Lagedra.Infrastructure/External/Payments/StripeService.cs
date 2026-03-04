using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Lagedra.Infrastructure.External.Payments;

public sealed partial class StripeService(
    IOptions<StripeSettings> settings,
    ILogger<StripeService> logger)
    : IStripeService
{
    private readonly StripeSettings _settings = settings.Value;

    public async Task<string> GetOrCreateCustomerAsync(Guid userId, string email, CancellationToken ct = default)
    {
        StripeConfiguration.ApiKey = _settings.SecretKey;
        var service = new CustomerService();
        var existing = await service.ListAsync(new CustomerListOptions { Email = email, Limit = 1 }, cancellationToken: ct).ConfigureAwait(false);

        if (existing.Data.Count > 0)
        {
            return existing.Data[0].Id;
        }

        var created = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Metadata = new Dictionary<string, string> { ["userId"] = userId.ToString() }
        }, cancellationToken: ct).ConfigureAwait(false);

        LogCustomerCreated(logger, userId, created.Id);
        return created.Id;
    }

    public async Task<StripeSubscriptionResult> CreateSubscriptionAsync(string customerId, string priceId, CancellationToken ct = default)
    {
        StripeConfiguration.ApiKey = _settings.SecretKey;
        var service = new SubscriptionService();
        var subscription = await service.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customerId,
            Items = [new SubscriptionItemOptions { Price = priceId }],
            PaymentBehavior = "default_incomplete",
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription"
            },
            Expand = ["latest_invoice.payment_intent"]
        }, cancellationToken: ct).ConfigureAwait(false);

        var clientSecret = subscription.LatestInvoice?.PaymentIntent?.ClientSecret ?? string.Empty;
        LogSubscriptionCreated(logger, customerId, subscription.Id);
        return new StripeSubscriptionResult(subscription.Id, clientSecret, subscription.CurrentPeriodEnd);
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default)
    {
        StripeConfiguration.ApiKey = _settings.SecretKey;
        var service = new SubscriptionService();
        await service.CancelAsync(subscriptionId, cancellationToken: ct).ConfigureAwait(false);
        LogSubscriptionCancelled(logger, subscriptionId);
    }

    public async Task<StripeInvoiceResult> CreateProratedInvoiceAsync(string subscriptionId, string priceId, CancellationToken ct = default)
    {
        StripeConfiguration.ApiKey = _settings.SecretKey;
        var subService = new SubscriptionService();
        var sub = await subService.GetAsync(subscriptionId, cancellationToken: ct).ConfigureAwait(false);

        var itemService = new SubscriptionItemService();
        var items = await itemService.ListAsync(new SubscriptionItemListOptions { Subscription = subscriptionId }, cancellationToken: ct).ConfigureAwait(false);

        await subService.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
        {
            Items = items.Data.Select(i => new SubscriptionItemOptions
            {
                Id = i.Id,
                Price = priceId
            }).ToList(),
            ProrationBehavior = "create_prorations"
        }, cancellationToken: ct).ConfigureAwait(false);

        var invoiceService = new InvoiceService();
        var invoice = await invoiceService.CreateAsync(new InvoiceCreateOptions
        {
            Customer = sub.CustomerId,
            Subscription = subscriptionId
        }, cancellationToken: ct).ConfigureAwait(false);

        return new StripeInvoiceResult(invoice.Id, invoice.AmountDue, invoice.Currency);
    }

    public Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        _ = ct;
        var webhookEvent = EventUtility.ConstructEvent(payload, signature, _settings.WebhookSecret);
        LogWebhookReceived(logger, webhookEvent.Type, webhookEvent.Id);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe customer created for user {UserId}: {CustomerId}")]
    private static partial void LogCustomerCreated(ILogger logger, Guid userId, string customerId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe subscription created for customer {CustomerId}: {SubscriptionId}")]
    private static partial void LogSubscriptionCreated(ILogger logger, string customerId, string subscriptionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe subscription cancelled: {SubscriptionId}")]
    private static partial void LogSubscriptionCancelled(ILogger logger, string subscriptionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stripe webhook received: {EventType} ({EventId})")]
    private static partial void LogWebhookReceived(ILogger logger, string eventType, string eventId);
}
