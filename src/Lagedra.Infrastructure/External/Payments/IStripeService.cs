namespace Lagedra.Infrastructure.External.Payments;

public sealed record StripeSubscriptionResult(string SubscriptionId, string ClientSecret, DateTime CurrentPeriodEnd);
public sealed record StripeInvoiceResult(string InvoiceId, long AmountDue, string Currency);

public interface IStripeService
{
    Task<string> GetOrCreateCustomerAsync(Guid userId, string email, CancellationToken ct = default);
    Task<StripeSubscriptionResult> CreateSubscriptionAsync(string customerId, string priceId, CancellationToken ct = default);
    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default);
    Task<StripeInvoiceResult> CreateProratedInvoiceAsync(string subscriptionId, string priceId, CancellationToken ct = default);
    Task HandleWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
