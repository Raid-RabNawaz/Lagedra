namespace Lagedra.Infrastructure.External.Payments;

public sealed record StripeSubscriptionResult(string SubscriptionId, string ClientSecret, DateTime CurrentPeriodEnd);
public sealed record StripeInvoiceResult(string InvoiceId, long AmountDue, string Currency);
public sealed record StripeConnectedAccountResult(string AccountId, Uri OnboardingUrl);
public sealed record StripeAccountStatusResult(
    string AccountId,
    bool ChargesEnabled,
    bool PayoutsEnabled,
    bool DetailsSubmitted,
    bool HasExternalAccount,
    bool HasOutstandingTaxRequirement,
    bool TaxRequirementPastDue,
    bool TaxRequirementPendingVerification,
    bool IsRestricted);
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

    /// <summary>
    /// Fetches the fixed unit amount (in the smallest currency unit, e.g. cents)
    /// of a Stripe Price. Used to reconcile the configured protocol-fee display
    /// value against the amount hosts are actually billed via the subscription
    /// price. Returns null if the price has no fixed unit amount (e.g. tiered).
    /// </summary>
    Task<long?> GetPriceAmountCentsAsync(string priceId, CancellationToken ct = default);
    Task<Stripe.Event> VerifyWebhookAsync(string payload, string signature);

    Task<StripeConnectedAccountResult> CreateConnectedAccountAsync(
        Guid hostUserId,
        string email,
        Uri? returnUrl = null,
        Uri? refreshUrl = null,
        CancellationToken ct = default);
    Task<Uri> CreateAccountOnboardingLinkAsync(string accountId, Uri? returnUrl = null, Uri? refreshUrl = null, CancellationToken ct = default);
    Task<StripeAccountStatusResult> GetAccountStatusAsync(string accountId, CancellationToken ct = default);

    /// <summary>
    /// Creates a destination-charge PaymentIntent where the host's connected
    /// account is the settlement merchant of record (<paramref name="onBehalfOf"/>).
    /// Stripe routes the funds (minus <paramref name="applicationFeeCents"/>) to the
    /// host, so the platform never holds the host's rent/deposit — the non-custodial
    /// model (Option A). <paramref name="onBehalfOf"/> is normally the same connected
    /// account as <paramref name="destinationAccountId"/>. The customer's statement
    /// shows the host's descriptor; <paramref name="statementDescriptorSuffix"/> is
    /// an optional dynamic suffix appended to it (card payments with
    /// <c>on_behalf_of</c> cannot set a full statement descriptor). When null the
    /// service falls back to the configured suffix.
    /// </summary>
    Task<StripePaymentIntentResult> CreateDestinationPaymentIntentAsync(
        long amountCents, string currency, string destinationAccountId,
        string onBehalfOf, long applicationFeeCents, Dictionary<string, string>? metadata = null,
        string? statementDescriptorSuffix = null, string? idempotencyKey = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a plain platform-charge PaymentIntent that settles into the
    /// platform's own Stripe balance (no connected account / <c>on_behalf_of</c>).
    /// Used for fees that are genuinely the platform's revenue for a service it
    /// provides directly — e.g. the arbitration filing fee — which is consistent
    /// with the non-custodial pillar because the platform is collecting its own
    /// money, not holding a host's rent/deposit.
    /// </summary>
    Task<StripePaymentIntentResult> CreatePlatformPaymentIntentAsync(
        long amountCents, string currency, Dictionary<string, string>? metadata = null,
        string? idempotencyKey = null, CancellationToken ct = default);

    Task<StripePaymentIntentResult> GetPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    /// <summary>
    /// Refunds a PaymentIntent. For destination charges the funds are now in the
    /// host's connected account, so set <paramref name="reverseTransfer"/> to claw
    /// the refunded amount back from the host and <paramref name="refundApplicationFee"/>
    /// to also return the platform's fee (used by cancellation / damage flows).
    /// </summary>
    Task<StripeRefundResult> RefundPaymentIntentAsync(
        string paymentIntentId,
        long? amountCents = null,
        bool reverseTransfer = false,
        bool refundApplicationFee = false,
        string? idempotencyKey = null,
        CancellationToken ct = default);

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
    /// Charges a saved payment method off-session against a connected destination
    /// account where the host is the settlement merchant of record
    /// (<paramref name="onBehalfOf"/>). Used by the apply/approve "card on file"
    /// flow to settle the booking the moment the host approves, without sending the
    /// tenant back to checkout. Non-custodial: Stripe routes funds (minus the
    /// application fee) to the host.
    /// </summary>
    Task<StripePaymentIntentResult> ChargeOffSessionDestinationAsync(
        string customerId,
        string paymentMethodId,
        long amountCents,
        string currency,
        string destinationAccountId,
        string onBehalfOf,
        long applicationFeeCents,
        Dictionary<string, string>? metadata = null,
        string? statementDescriptorSuffix = null,
        string? idempotencyKey = null,
        CancellationToken ct = default);
}
