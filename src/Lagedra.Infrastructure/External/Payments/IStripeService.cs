namespace Lagedra.Infrastructure.External.Payments;

public sealed record StripeSubscriptionResult(string SubscriptionId, string ClientSecret, DateTime CurrentPeriodEnd);
public sealed record StripeInvoiceResult(string InvoiceId, long AmountDue, string Currency);
public sealed record StripeConnectedAccountResult(string AccountId, Uri OnboardingUrl);
public sealed record StripeAccountStatusResult(string AccountId, bool ChargesEnabled, bool PayoutsEnabled, bool DetailsSubmitted);
public sealed record StripePaymentIntentResult(string PaymentIntentId, string ClientSecret, string Status, long Amount, string Currency);
public sealed record StripeRefundResult(string RefundId, long AmountRefunded, string Status);

/// <summary>
/// Phase 16.9: Stripe SetupIntent client material returned to the frontend
/// so the tenant can confirm a payment-method save off-session at apply time.
/// </summary>
public sealed record StripeSetupIntentResult(string SetupIntentId, string ClientSecret, string Status);

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

    /// <summary>
    /// Creates a SetupIntent for the supplied customer so the frontend can
    /// confirm a card save (off-session usage) during the apply flow. The
    /// resulting payment-method id is later attached to the application and
    /// charged from the host's approve action without a second tenant prompt
    /// (Phase 16.9 "card on file").
    /// </summary>
    Task<StripeSetupIntentResult> CreateSetupIntentAsync(
        string customerId,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null,
        CancellationToken ct = default);

    /// <summary>
    /// Charges a previously saved payment method off-session against the
    /// platform Stripe account (direct-payout host model). Used by
    /// <c>ApproveDealApplicationCommand</c> to settle the booking the moment
    /// the host approves, without sending the tenant back to the checkout
    /// surface.
    /// </summary>
    Task<StripePaymentIntentResult> ChargeOffSessionPlatformAsync(
        string customerId,
        string paymentMethodId,
        long amountCents,
        string currency,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null,
        CancellationToken ct = default);

    /// <summary>
    /// Charges a saved payment method off-session against a connected
    /// destination account (Stripe Connect host model). Mirror of
    /// <see cref="ChargeOffSessionPlatformAsync"/> for hosts who haven't
    /// switched to direct payouts.
    /// </summary>
    Task<StripePaymentIntentResult> ChargeOffSessionDestinationAsync(
        string customerId,
        string paymentMethodId,
        long amountCents,
        string currency,
        string destinationAccountId,
        long applicationFeeCents,
        Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null,
        CancellationToken ct = default);
}
