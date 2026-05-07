namespace Lagedra.Infrastructure.External.Payments;

public sealed record StripeSubscriptionResult(string SubscriptionId, string ClientSecret, DateTime CurrentPeriodEnd);
public sealed record StripeInvoiceResult(string InvoiceId, long AmountDue, string Currency);
public sealed record StripeConnectedAccountResult(string AccountId, Uri OnboardingUrl);
public sealed record StripeAccountStatusResult(string AccountId, bool ChargesEnabled, bool PayoutsEnabled, bool DetailsSubmitted);
public sealed record StripePaymentIntentResult(string PaymentIntentId, string ClientSecret, string Status, long Amount, string Currency);
public sealed record StripeRefundResult(string RefundId, long AmountRefunded, string Status);

public interface IStripeService
{
    Task<string> GetOrCreateCustomerAsync(Guid userId, string email, CancellationToken ct = default);
    Task<StripeSubscriptionResult> CreateSubscriptionAsync(string customerId, string priceId, string? idempotencyKey = null, CancellationToken ct = default);
    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default);
    Task<StripeInvoiceResult> CreateProratedInvoiceAsync(string subscriptionId, string priceId, CancellationToken ct = default);
    Task<Stripe.Event> VerifyWebhookAsync(string payload, string signature);

    Task<StripeConnectedAccountResult> CreateConnectedAccountAsync(Guid hostUserId, string email, CancellationToken ct = default);
    Task<Uri> CreateAccountOnboardingLinkAsync(string accountId, Uri? returnUrl = null, Uri? refreshUrl = null, CancellationToken ct = default);
    Task<StripeAccountStatusResult> GetAccountStatusAsync(string accountId, CancellationToken ct = default);
    Task<StripePaymentIntentResult> CreateDestinationPaymentIntentAsync(
        long amountCents, string currency, string destinationAccountId,
        long applicationFeeCents, Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a PaymentIntent that settles entirely to the platform's Stripe account
    /// (no destination/transfer). Used for the Airbnb-style direct-payout model where the
    /// host receives funds out-of-band via their saved payout instructions.
    /// </summary>
    Task<StripePaymentIntentResult> CreatePlatformPaymentIntentAsync(
        long amountCents, string currency,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null, CancellationToken ct = default);
    Task<StripePaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);
    Task<StripeRefundResult> RefundPaymentIntentAsync(string paymentIntentId, long? amountCents = null, string? idempotencyKey = null, CancellationToken ct = default);
    Task<bool> CheckConnectivityAsync(CancellationToken ct = default);
}
