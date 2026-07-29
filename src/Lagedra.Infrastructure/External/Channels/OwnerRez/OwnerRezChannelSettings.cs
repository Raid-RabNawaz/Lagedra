namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>
/// Static, environment-level configuration for the OwnerRez API v2 integration.
/// Per-host credentials (account email + personal access token) live on the
/// <c>ChannelConnection</c>, not here — OwnerRez v2 authenticates per account,
/// so there is no shared platform key.
/// </summary>
public sealed class OwnerRezChannelSettings
{
    public const string SectionName = "Channels:OwnerRez";

    public Uri BaseUrl { get; init; } = new("https://api.ownerrez.com");

    public string UserAgent { get; init; } = "Lagedra/1.0 (+https://lagedra.com)";

    /// <summary>
    /// Page size for list calls. OwnerRez caps <c>limit</c> at 100.
    /// </summary>
    public int PageSize { get; init; } = 100;

    /// <summary>
    /// How far ahead to pull bookings when deriving an availability calendar (days).
    /// </summary>
    public int AvailabilityLookaheadDays { get; init; } = 365;

    /// <summary>
    /// Safety valve on paged reads so a misbehaving feed can never loop forever.
    /// </summary>
    public int MaxPages { get; init; } = 50;
}
