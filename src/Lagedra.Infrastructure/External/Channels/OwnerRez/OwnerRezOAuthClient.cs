using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>A token set issued by OwnerRez for one host account.</summary>
/// <param name="AccessToken">Bearer token for API calls. Starts with <c>at_</c>.</param>
/// <param name="RefreshToken">
/// Present only when the app uses the standard (expiring) token policy.
/// </param>
/// <param name="ExpiresAt">Null when the token never expires.</param>
/// <param name="UserId">The OwnerRez user id the token belongs to.</param>
public sealed record OwnerRezTokenSet(
    string AccessToken,
    string? RefreshToken,
    DateTime? ExpiresAt,
    string UserId,
    string? UserDisplayName);

/// <summary>
/// OAuth 2.0 authorization-code client for the OwnerRez app, per
/// https://www.ownerrez.com/support/articles/api-oauth-app. Client credentials are
/// sent as HTTP Basic on the token endpoint; the issued access token is what the
/// per-host API calls then use as a bearer token.
/// </summary>
public sealed partial class OwnerRezOAuthClient(
    HttpClient httpClient,
    IOptions<OwnerRezChannelSettings> settings,
    ILogger<OwnerRezOAuthClient> logger)
{
    private readonly OwnerRezChannelSettings _settings = settings.Value;

    /// <summary>
    /// Where to send the host to approve access. <paramref name="state"/> is
    /// returned verbatim on the callback and must be verified there.
    /// </summary>
    public Uri BuildAuthorizationUrl(Uri redirectUri, string state)
    {
        ArgumentNullException.ThrowIfNull(redirectUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        var query = string.Join('&',
        [
            "response_type=code",
            $"client_id={Uri.EscapeDataString(_settings.ClientId)}",
            $"redirect_uri={Uri.EscapeDataString(redirectUri.ToString())}",
            $"state={Uri.EscapeDataString(state)}",
        ]);

        return new Uri($"{_settings.AuthorizeUrl}?{query}");
    }

    /// <summary>Exchanges the one-time code from the callback for an access token.</summary>
    public Task<OwnerRezTokenSet?> ExchangeCodeAsync(
        string code,
        Uri redirectUri,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(redirectUri);

        return PostTokenAsync(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri.ToString(),
            },
            nameof(ExchangeCodeAsync),
            ct);
    }

    /// <summary>Renews an access token that is expiring, or has already expired.</summary>
    public Task<OwnerRezTokenSet?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        return PostTokenAsync(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
            },
            nameof(RefreshAsync),
            ct);
    }

    /// <summary>
    /// Tells OwnerRez to forget a token, so disconnecting in Lagedra also removes
    /// Lagedra's access from the host's OwnerRez account. Best-effort.
    /// </summary>
    public async Task<bool> RevokeAsync(string accessToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Delete, $"/oauth/access_token/{Uri.EscapeDataString(accessToken)}");
            request.Headers.Authorization = ClientAuthorization();

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogRevokeFailed(logger, (int)response.StatusCode);
                return false;
            }

            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            LogOAuthRequestFailed(logger, nameof(RevokeAsync), ex);
            return false;
        }
    }

    private async Task<OwnerRezTokenSet?> PostTokenAsync(
        Dictionary<string, string> form,
        string operation,
        CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/oauth/access_token")
            {
                Content = new FormUrlEncodedContent(form),
            };
            request.Headers.Authorization = ClientAuthorization();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogTokenRejected(logger, operation, (int)response.StatusCode, Describe(payload));
                return null;
            }

            return Parse(payload);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogOAuthRequestFailed(logger, operation, ex);
            return null;
        }
    }

    private AuthenticationHeaderValue ClientAuthorization()
    {
        var raw = $"{_settings.ClientId}:{_settings.ClientSecret}";
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(raw)));
    }

    private static OwnerRezTokenSet? Parse(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        var accessToken = String(root, "access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        // user_id is numeric in OwnerRez responses; keep it as text since it is
        // only ever used as an opaque account identifier.
        var userId = root.TryGetProperty("user_id", out var userEl)
            ? userEl.ToString()
            : string.Empty;

        return new OwnerRezTokenSet(
            accessToken,
            String(root, "refresh_token"),
            ResolveExpiry(root),
            userId,
            String(root, "user_display_name"));
    }

    /// <summary>
    /// Prefers the absolute <c>expires_at</c> and falls back to <c>expires_in</c>
    /// seconds. Neither being present means the app is on the legacy non-expiring
    /// token policy, so there is nothing to renew.
    /// </summary>
    private static DateTime? ResolveExpiry(JsonElement root)
    {
        if (root.TryGetProperty("expires_at", out var atEl)
            && atEl.ValueKind == JsonValueKind.String
            && DateTime.TryParse(
                atEl.GetString(), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var expiresAt))
        {
            return expiresAt;
        }

        if (root.TryGetProperty("expires_in", out var inEl)
            && inEl.ValueKind == JsonValueKind.Number
            && inEl.TryGetInt64(out var seconds)
            && seconds > 0)
        {
            return DateTime.UtcNow.AddSeconds(seconds);
        }

        return null;
    }

    private static string? String(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// OwnerRez returns <c>error</c> / <c>error_description</c> on failure. Only the
    /// machine-readable code is logged; the body can echo the submitted token.
    /// </summary>
    private static string Describe(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return "(empty)";
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            return String(doc.RootElement, "error") ?? "(unknown)";
        }
        catch (JsonException)
        {
            return "(unparseable)";
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[OwnerRez OAuth] {Operation} rejected with HTTP {StatusCode}: {Error}")]
    private static partial void LogTokenRejected(
        ILogger logger, string operation, int statusCode, string error);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[OwnerRez OAuth] token revoke returned HTTP {StatusCode}")]
    private static partial void LogRevokeFailed(ILogger logger, int statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[OwnerRez OAuth] {Operation} failed")]
    private static partial void LogOAuthRequestFailed(ILogger logger, string operation, Exception ex);
}
