namespace Lagedra.Infrastructure.External.Channels.Smoobu;

/// <summary>
/// Environment-level configuration for the Smoobu channel integration.
/// Per-host credentials (API key + API secret for HMAC request signing) live on
/// <c>ChannelConnection</c>, not here — Smoobu issues them per user under
/// Settings → Advanced → API Keys.
/// </summary>
public sealed class SmoobuChannelSettings
{
    public const string SectionName = "Channels:Smoobu";

    /// <summary>Smoobu API host (all resources live under /api and /booking).</summary>
    public Uri BaseUrl { get; init; } = new("https://login.smoobu.com");

    public string UserAgent { get; init; } = "Lagedra/1.0 (+https://lagedra.com)";

    /// <summary>
    /// Smoobu <c>channelId</c> stamped on reservations we create.
    /// <c>70</c> is "Homepage", the default channel Smoobu documents for
    /// API-created bookings.
    /// </summary>
    public int DefaultChannelId { get; init; } = 70;

    /// <summary>How far ahead to pull calendars when syncing availability (days).</summary>
    public int AvailabilityLookaheadDays { get; init; } = 365;

    /// <summary>Page size for reservation list calls (Smoobu max is 100).</summary>
    public int PageSize { get; init; } = 100;

    /// <summary>Safety cap on pages fetched per reservation-list sync.</summary>
    public int MaxPages { get; init; } = 50;
}
