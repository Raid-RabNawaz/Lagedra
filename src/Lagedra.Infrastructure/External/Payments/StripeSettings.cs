namespace Lagedra.Infrastructure.External.Payments;

public sealed class StripeSettings
{
    public const string SectionName = "Stripe";

    public required string PublishableKey { get; init; }
    public required string SecretKey { get; init; }
    public required string WebhookSecret { get; init; }
    public string ApiVersion { get; init; } = "2024-12-18.acacia";
}
