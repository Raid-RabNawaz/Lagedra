namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>
/// Static, environment-level configuration for the OwnerRez API v2 integration.
///
/// Hosts authorize Lagedra through an OwnerRez OAuth app, so the platform-level
/// values here are the app's <see cref="ClientId"/> and <see cref="ClientSecret"/>;
/// the resulting per-host access token lives on the <c>ChannelConnection</c>.
/// (OwnerRez personal access tokens are capped at two accounts per IP per day and
/// are explicitly not for partner use, so they are unusable for a multi-host
/// platform — see https://www.ownerrez.com/support/articles/api-auth.)
/// </summary>
public sealed class OwnerRezChannelSettings
{
    public const string SectionName = "Channels:OwnerRez";

    public Uri BaseUrl { get; init; } = new("https://api.ownerrez.com");

    /// <summary>
    /// Where hosts are sent to approve the app. Lives on <c>app.ownerrez.com</c>
    /// rather than the API host because it needs the host's logged-in session.
    /// </summary>
    public Uri AuthorizeUrl { get; init; } = new("https://app.ownerrez.com/oauth/authorize");

    /// <summary>OAuth app client id, issued by OwnerRez. Starts with <c>c_</c>.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>OAuth app client secret, issued by OwnerRez. Starts with <c>s_</c>.</summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// OwnerRez requires a User-Agent naming the app and its client id; the client
    /// id is appended automatically, so configure only the product part here.
    /// </summary>
    public string UserAgent { get; init; } = "Lagedra/1.0";

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

    /// <summary>
    /// How early to renew an access token before it expires. OwnerRez's standard
    /// policy issues 30-day tokens, so a week of slack tolerates a few missed job
    /// runs without a host having to reconnect.
    /// </summary>
    public int TokenRefreshLeadDays { get; init; } = 7;

    /// <summary>
    /// Basic-auth username OwnerRez sends on webhook deliveries, matching the User
    /// field on the OAuth app's Webhooks section. Deliveries are rejected while
    /// this and <see cref="WebhookPassword"/> are unset, because the payloads can
    /// cancel bookings and an unauthenticated endpoint would let anyone forge them.
    /// </summary>
    public string WebhookUsername { get; init; } = string.Empty;

    /// <summary>Basic-auth password OwnerRez sends on webhook deliveries.</summary>
    public string WebhookPassword { get; init; } = string.Empty;

    public bool IsWebhookAuthConfigured =>
        !string.IsNullOrWhiteSpace(WebhookUsername) && !string.IsNullOrWhiteSpace(WebhookPassword);

    /// <summary>
    /// How long a host has to complete the OwnerRez consent screen. OwnerRez
    /// expires the temporary code after 10 minutes, so matching that keeps the
    /// state we round-trip from outliving the code it pairs with.
    /// </summary>
    public int AuthorizationStateLifetimeMinutes { get; init; } = 10;

    public bool IsOAuthConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    /// <summary>Full User-Agent header value, e.g. <c>Lagedra/1.0 (c_1234)</c>.</summary>
    public string BuildUserAgent() =>
        string.IsNullOrWhiteSpace(ClientId) ? UserAgent : $"{UserAgent} ({ClientId})";
}
