namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>
/// Static, environment-level configuration for the OwnerRez channel
/// integration. Per-host credentials (advertiser id, username, secret) live on
/// the <c>ChannelConnection</c>, not here.
/// </summary>
public sealed class OwnerRezChannelSettings
{
    public const string SectionName = "Channels:OwnerRez";

    public Uri BaseUrl { get; init; } = new("https://faststage.ownerrez.com");

    /// <summary>
    /// Channel-level API username issued by OwnerRez (HTTP Basic auth user).
    /// Sandbox MOR value: "HaXmlSandboxMoR".
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Channel-level API key issued by OwnerRez (HTTP Basic auth password).
    /// </summary>
    public string Key { get; init; } = string.Empty;

    public string UserAgent { get; init; } = "Lagedra/1.0 (+https://lagedra.com)";

    /// <summary>
    /// Stable identifier OwnerRez echoes back on OLB calls (the
    /// <c>systemExternalId</c> field). Identifies our channel system.
    /// </summary>
    public string SystemExternalId { get; init; } = "Lagedra";
}
