namespace Lagedra.Infrastructure.External.Channels.Hostaway;

/// <summary>
/// Static, environment-level configuration for the Hostaway channel integration.
/// Per-host credentials (account ID + API client secret) live on the
/// <c>ChannelConnection</c>, not here — Hostaway uses OAuth2 client-credentials
/// per account rather than a shared platform key.
/// </summary>
public sealed class HostawayChannelSettings
{
    public const string SectionName = "Channels:Hostaway";

    public Uri BaseUrl { get; init; } = new("https://api.hostaway.com");

    public string UserAgent { get; init; } = "Lagedra/1.0 (+https://lagedra.com)";

    /// <summary>
    /// Hostaway <c>channelId</c> stamped on reservations we create.
    /// <c>2000</c> is the Direct / booking-engine channel used for API-created
    /// bookings in Hostaway's public docs.
    /// </summary>
    public int DefaultChannelId { get; init; } = 2000;

    /// <summary>
    /// How far ahead to pull calendars when syncing availability (days).
    /// </summary>
    public int AvailabilityLookaheadDays { get; init; } = 365;

    /// <summary>
    /// Page size for listing / reservation list calls (Hostaway max is typically 100+).
    /// </summary>
    public int PageSize { get; init; } = 100;

    /// <summary>
    /// When true, the first successful Hostaway sync registers a unified webhook
    /// pointing at Lagedra's <c>/v1/webhooks/hostaway</c> endpoint (if missing).
    /// </summary>
    public bool AutoRegisterWebhooks { get; init; } = true;

    /// <summary>
    /// Optional Basic-auth username Hostaway should send on webhook deliveries.
    /// Also used when auto-registering the unified webhook.
    /// </summary>
    public string WebhookUsername { get; init; } = string.Empty;

    /// <summary>
    /// Optional Basic-auth password Hostaway should send on webhook deliveries.
    /// </summary>
    public string WebhookPassword { get; init; } = string.Empty;
}
