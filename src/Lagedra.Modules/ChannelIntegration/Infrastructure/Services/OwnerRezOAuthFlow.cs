using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>
/// The parts of the OwnerRez authorization-code flow that both ends of it need to
/// agree on: the redirect URI (OwnerRez rejects a mismatch between the authorize
/// request and the token exchange) and the <c>state</c> parameter.
///
/// State is a self-contained encrypted token rather than a database row. It is
/// authenticated encryption (AES-GCM), so a state Lagedra did not mint cannot be
/// forged, which is exactly the cross-site request forgery protection state exists
/// for — without a table to write, expire, and clean up.
/// </summary>
public sealed class OwnerRezOAuthFlow(
    IConfiguration configuration,
    IEncryptionService encryption,
    IOptions<OwnerRezChannelSettings> settings,
    IClock clock)
{
    public const string ProviderKey = "ownerrez";

    private readonly OwnerRezChannelSettings _settings = settings.Value;

    public bool IsConfigured => _settings.IsOAuthConfigured;

    /// <summary>
    /// Where OwnerRez sends the host back to. Points at the API rather than the
    /// SPA because the callback has to exchange the code using the client secret.
    /// Must match the OAuth Redirect URL registered with OwnerRez, which requires
    /// HTTPS — so local testing needs a tunnelled <c>App:BaseUrl</c>.
    /// </summary>
    public Uri RedirectUri
    {
        get
        {
            var baseUrl = (configuration["App:BaseUrl"] ?? "http://localhost:5000").TrimEnd('/');
            return new Uri($"{baseUrl}/v1/channels/ownerrez/oauth/callback");
        }
    }

    public string CreateState(Guid hostUserId)
    {
        var payload = JsonSerializer.Serialize(new StatePayload(
            hostUserId,
            clock.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))));

        return encryption.Encrypt(payload);
    }

    /// <summary>
    /// Returns the host the state was minted for, or null if it was tampered with,
    /// truncated, encrypted under a different key, or has gone stale.
    /// </summary>
    public Guid? TryReadState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        StatePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<StatePayload>(encryption.Decrypt(state));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException or JsonException)
        {
            return null;
        }

        if (payload is null
            || payload.HostUserId == Guid.Empty
            || !DateTime.TryParse(
                payload.IssuedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var issuedAt))
        {
            return null;
        }

        var age = clock.UtcNow - issuedAt;
        var lifetime = TimeSpan.FromMinutes(Math.Max(1, _settings.AuthorizationStateLifetimeMinutes));

        return age >= TimeSpan.Zero && age <= lifetime ? payload.HostUserId : null;
    }

    private sealed record StatePayload(Guid HostUserId, string IssuedAt, string Nonce);
}
