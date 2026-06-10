using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lagedra.SharedKernel.Caching;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.Security;

public sealed class ActionTokenSettings
{
    public const string SectionName = "ActionTokens";

    /// <summary>
    /// HMAC-SHA256 secret used to sign action tokens. Must be at least
    /// 32 chars in production. Falls back to the JWT secret if unset
    /// (backwards-compat: keeps single-secret deployments working).
    /// </summary>
    public string Secret { get; set; } = string.Empty;
}

/// <summary>
/// Phase 16.10 — HMAC-signed compact action tokens. Format:
///   base64url(payload).base64url(hmac)
/// Payload is JSON: <c>{ "act": "approve_app", "sub": "{guid}", "uid": "{guid}", "exp": &lt;unix-secs&gt; }</c>.
/// </summary>
public sealed class ActionTokenService(
    IOptions<ActionTokenSettings> settings,
    IClock clock,
    ICacheService cache)
    : IActionTokenService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Cache key prefix for the consumed-tokens nonce store. Keys are
    /// SHA-256(token) so the raw token never lands in cache (defence in
    /// depth — even if someone dumps the cache, they only get hashes).
    /// </summary>
    private const string ConsumedKeyPrefix = "actiontoken:consumed:";

    private readonly byte[] _key = Encoding.UTF8.GetBytes(
        string.IsNullOrEmpty(settings.Value.Secret)
            ? throw new InvalidOperationException(
                "ActionTokens:Secret is not configured. Set it in appsettings or env.")
            : settings.Value.Secret);

    public string Issue(string action, Guid subjectId, Guid principalUserId, TimeSpan ttl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "TTL must be positive.");
        }

        var expiresAt = clock.UtcNow.Add(ttl);
        var payload = new TokenPayloadEnvelope
        {
            act = action,
            sub = subjectId.ToString("N"),
            uid = principalUserId.ToString("N"),
            exp = new DateTimeOffset(expiresAt, TimeSpan.Zero).ToUnixTimeSeconds(),
        };

        var payloadJson = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
        var payloadSegment = Base64UrlEncode(payloadJson);
        var hmacSegment = Base64UrlEncode(ComputeHmac(payloadSegment));
        return $"{payloadSegment}.{hmacSegment}";
    }

    public ActionTokenValidationResult Validate(string token, string expectedAction)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ActionTokenValidationResult.Failure("token.missing", "Token is missing.");
        }

        var parts = token.Split('.');
        if (parts.Length != 2)
        {
            return ActionTokenValidationResult.Failure("token.malformed", "Token is malformed.");
        }

        byte[] payloadBytes;
        byte[] suppliedSig;
        try
        {
            payloadBytes = Base64UrlDecode(parts[0]);
            suppliedSig = Base64UrlDecode(parts[1]);
        }
        catch (FormatException)
        {
            return ActionTokenValidationResult.Failure("token.malformed", "Token is malformed.");
        }

        var expectedSig = ComputeHmac(parts[0]);
        if (!CryptographicOperations.FixedTimeEquals(expectedSig, suppliedSig))
        {
            return ActionTokenValidationResult.Failure("token.invalid_signature", "Token signature is invalid.");
        }

        TokenPayloadEnvelope? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TokenPayloadEnvelope>(payloadBytes, JsonOptions);
        }
        catch (JsonException)
        {
            return ActionTokenValidationResult.Failure("token.malformed", "Token payload is malformed.");
        }

        if (payload is null
            || string.IsNullOrEmpty(payload.act)
            || string.IsNullOrEmpty(payload.sub)
            || string.IsNullOrEmpty(payload.uid))
        {
            return ActionTokenValidationResult.Failure("token.malformed", "Token payload is incomplete.");
        }

        if (!string.Equals(payload.act, expectedAction, StringComparison.Ordinal))
        {
            return ActionTokenValidationResult.Failure(
                "token.wrong_action",
                $"Token is for action '{payload.act}', not '{expectedAction}'.");
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(payload.exp);
        if (clock.UtcNow >= expiresAt.UtcDateTime)
        {
            return ActionTokenValidationResult.Failure("token.expired", "Token has expired.");
        }

        if (!Guid.TryParseExact(payload.sub, "N", out var subject)
            || !Guid.TryParseExact(payload.uid, "N", out var principal))
        {
            return ActionTokenValidationResult.Failure("token.malformed", "Token ids are malformed.");
        }

        return ActionTokenValidationResult.Success(
            new ActionTokenPayload(payload.act, subject, principal, expiresAt));
    }

    public async Task<ActionTokenValidationResult> ValidateAndConsumeAsync(
        string token,
        string expectedAction,
        CancellationToken cancellationToken = default)
    {
        // Validate first — it's pure, cheap, and gives us the expiry
        // we'll need to bound the nonce TTL.
        var result = Validate(token, expectedAction);
        if (!result.IsValid || result.Payload is null)
        {
            return result;
        }

        // Single-use enforcement: stamp the token's hash into the cache
        // for the rest of its TTL. Subsequent presentations of the same
        // token return token.alreadyUsed even though the signature still
        // validates. There's a tiny TOCTOU window between the TryGet
        // and SetAsync calls — acceptable for an email-link flow where
        // the realistic threat is "user double-clicks the email", not a
        // concurrent attacker. A distributed cache backed by Redis SETNX
        // closes that window when we move off in-memory.
        var key = ConsumedKeyPrefix + ComputeTokenFingerprint(token);

        var alreadyConsumed = await cache.GetAsync<string>(key, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyConsumed is not null)
        {
            return ActionTokenValidationResult.Failure(
                "token.alreadyUsed",
                "This one-tap action link has already been used.");
        }

        var ttl = result.Payload.ExpiresAt.UtcDateTime - clock.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return ActionTokenValidationResult.Failure("token.expired", "Token has expired.");
        }

        await cache.SetAsync(key, "1", ttl, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private static string ComputeTokenFingerprint(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
        {
            sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    private byte[] ComputeHmac(string payloadSegment)
    {
        using var hmac = new HMACSHA256(_key);
        return hmac.ComputeHash(Encoding.ASCII.GetBytes(payloadSegment));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        Span<byte> buffer = stackalloc byte[Base64.GetMaxEncodedToUtf8Length(bytes.Length)];
        Base64.EncodeToUtf8(bytes, buffer, out _, out var written);
        // strip padding and translate to URL-safe alphabet
        var s = Encoding.ASCII.GetString(buffer[..written]).TrimEnd('=');
        return s.Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

#pragma warning disable IDE1006 // Lower-case fields match the wire payload contract.
    private sealed record TokenPayloadEnvelope
    {
        public string act { get; init; } = string.Empty;
        public string sub { get; init; } = string.Empty;
        public string uid { get; init; } = string.Empty;
        public long exp { get; init; }
    }
#pragma warning restore IDE1006
}
