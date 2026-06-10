namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Snapshot of the caller's consent state used by the booking pre-flight
/// (Phase 16) and the global consent middleware. <see cref="HasRequired"/>
/// is true only when every required consent has been granted (and not
/// withdrawn). <see cref="MissingConsentTypes"/> lists the human-readable
/// names of the consents the user still owes us.
/// </summary>
public sealed record ConsentStatus(
    bool HasRequired,
    IReadOnlyList<string> MissingConsentTypes);

public interface IConsentChecker
{
    Task<bool> HasRequiredConsentsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns both <c>hasRequired</c> and the list of consents the user is
    /// still missing. Used by <c>GET /v1/privacy/consents/me/status</c> for
    /// the booking pre-flight banner so we can deep-link to the exact
    /// consent the user needs to grant.
    /// </summary>
    Task<ConsentStatus> GetRequiredConsentStatusAsync(
        Guid userId,
        CancellationToken ct = default);
}
