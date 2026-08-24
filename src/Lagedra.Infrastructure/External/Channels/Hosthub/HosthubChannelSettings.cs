namespace Lagedra.Infrastructure.External.Channels.Hosthub;

/// <summary>
/// Environment-level configuration for the Hosthub public API.
/// Per-host credentials (the account owner's API key) live on
/// <c>ChannelConnection</c>, not here.
/// </summary>
public sealed class HosthubChannelSettings
{
    public const string SectionName = "Channels:Hosthub";

    /// <summary>
    /// Hosthub origin only (no <c>/api/…</c> path). Production is
    /// <c>https://app.hosthub.com</c>; Hosthub's own staging is
    /// <c>https://eric.hosthub.com</c> and is selected via this setting.
    /// </summary>
    public Uri BaseUrl { get; init; } = new("https://app.hosthub.com");

    public string UserAgent { get; init; } = "Lagedra/1.0 (+https://lagedra.com)";

    /// <summary>
    /// Hosthub API version segment. <c>2019-03-01</c> is current; a breaking
    /// change would ship under a new date and keep this one working.
    /// </summary>
    public string ApiVersion { get; init; } = "2019-03-01";

    /// <summary>How far ahead to treat calendar events as availability (days).</summary>
    public int AvailabilityLookaheadDays { get; init; } = 365;

    /// <summary>Safety cap on cursor pages fetched per list call.</summary>
    public int MaxPages { get; init; } = 50;

    /// <summary>
    /// Optional Hosthub <c>source_id</c> stamped on bookings we create so they
    /// show as Lagedra in Hosthub. Empty omits the field.
    /// </summary>
    public string SourceId { get; init; } = string.Empty;
}
