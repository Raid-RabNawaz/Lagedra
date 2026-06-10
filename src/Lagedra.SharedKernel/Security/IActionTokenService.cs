namespace Lagedra.SharedKernel.Security;

/// <summary>
/// Phase 16.10 — issues and validates short-lived HMAC-signed tokens
/// the platform embeds in transactional emails so the host can take a
/// single sensitive action (e.g. approve an application) by clicking
/// a deep link, without juggling the regular auth flow.
/// </summary>
public interface IActionTokenService
{
    /// <summary>
    /// Mints a token for the given action. The same caller, action,
    /// and subject pair will produce different tokens each call (no
    /// idempotency) — caller is responsible for marking actions
    /// already-taken in the domain.
    /// </summary>
    string Issue(string action, Guid subjectId, Guid principalUserId, TimeSpan ttl);

    /// <summary>
    /// Validates the supplied token against the expected action label.
    /// Returns the decoded payload on success, or a structured failure
    /// (expired / invalid signature / wrong action / malformed).
    /// Pure function — does NOT mark the token as consumed.
    /// </summary>
    ActionTokenValidationResult Validate(string token, string expectedAction);

    /// <summary>
    /// Validates the token *and* atomically marks it as consumed so
    /// the same link can't be POSTed twice (replay protection).
    /// Returns <c>token.alreadyUsed</c> when the token has already
    /// been consumed during its TTL.
    /// </summary>
    Task<ActionTokenValidationResult> ValidateAndConsumeAsync(
        string token,
        string expectedAction,
        CancellationToken cancellationToken = default);
}

public sealed record ActionTokenPayload(
    string Action,
    Guid SubjectId,
    Guid PrincipalUserId,
    DateTimeOffset ExpiresAt);

public sealed record ActionTokenValidationResult(
    bool IsValid,
    ActionTokenPayload? Payload,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static ActionTokenValidationResult Success(ActionTokenPayload payload) =>
        new(true, payload, null, null);

    public static ActionTokenValidationResult Failure(string code, string message) =>
        new(false, null, code, message);
}
