namespace Lagedra.Infrastructure.External.Payments;

public sealed class StripeSettings
{
    public const string SectionName = "Stripe";

    public required string PublishableKey { get; init; }
    public required string SecretKey { get; init; }
    public required string WebhookSecret { get; init; }
    public string ApiVersion { get; init; } = "2024-12-18.acacia";
    public Uri ConnectReturnUrl { get; set; } = new("http://localhost:3000/app/payout-setup");
    public Uri ConnectRefreshUrl { get; set; } = new("http://localhost:3000/app/payout-setup");

    /// <summary>
    /// Optional dynamic statement-descriptor suffix for booking charges. Booking
    /// charges are destination charges with <c>on_behalf_of</c> (the host is the
    /// settlement merchant), so the customer's statement shows the host's
    /// descriptor as the static component; for card payments Stripe only allows a
    /// <c>statement_descriptor_suffix</c> (never a full <c>statement_descriptor</c>).
    /// Leave empty to show only the host's descriptor (most aligned with the
    /// non-custodial "pay the host directly" model).
    /// </summary>
    public string? StatementDescriptorSuffix { get; init; }

    /// <summary>
    /// Hard allow-list of Stripe payment-method types offered to tenants in the
    /// PaymentElement (booking checkout and the arbitration filing fee). Only
    /// these render — regardless of what's enabled in the Stripe Dashboard or
    /// which local rails the customer's browser locale/geo would otherwise
    /// surface via Stripe "automatic payment methods".
    /// <para>
    /// This exists because automatic methods advertised wrong-region rails
    /// (Pix, Kakao Pay, Naver Pay) to US tenants — noise that erodes credibility
    /// at the point of payment.
    /// </para>
    /// <para>
    /// Defaults to <c>card</c> only, the one method guaranteed to work with the
    /// current Connect wiring. To also offer US bank debit, add
    /// <c>us_bank_account</c> here <b>and</b> ensure the
    /// <c>us_bank_account_ach_payments</c> capability is active on the platform
    /// account and on every host connected account — booking charges are
    /// destination charges with <c>on_behalf_of</c>, so the host is the ACH
    /// settlement merchant (see <see cref="IStripeService"/>). Leaving that
    /// capability unrequested will make the PaymentIntent fail. Set to an empty
    /// array to fall back to Stripe automatic payment methods.
    /// </para>
    /// </summary>
    public IReadOnlyList<string> PaymentMethodTypes { get; init; } = ["card"];
}
