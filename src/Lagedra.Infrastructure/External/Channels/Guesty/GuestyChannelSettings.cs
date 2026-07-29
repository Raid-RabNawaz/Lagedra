namespace Lagedra.Infrastructure.External.Channels.Guesty;

/// <summary>
/// Environment-level configuration for the Guesty Open API integration.
/// Per-host credentials (OAuth Client ID + Client Secret) live on
/// <c>ChannelConnection</c>, not here.
/// </summary>
public sealed class GuestyChannelSettings
{
    public const string SectionName = "Channels:Guesty";

    /// <summary>Guesty Open API host (token is at /oauth2/token; resources under /v1).</summary>
    public Uri BaseUrl { get; init; } = new("https://open-api.guesty.com");

    public string UserAgent { get; init; } = "Lagedra/1.0 (+https://lagedra.com)";

    /// <summary>How far ahead to pull calendars when syncing availability (days).</summary>
    public int AvailabilityLookaheadDays { get; init; } = 365;

    /// <summary>Page size for listing / reservation list calls (Guesty max is typically 100).</summary>
    public int PageSize { get; init; } = 100;

    /// <summary>
    /// Reservation <c>source</c> stamped on bookings we push.
    /// Guesty accepts custom strings; <c>manual</c> is the safest default for Open API.
    /// </summary>
    public string ReservationSource { get; init; } = "manual";
}
