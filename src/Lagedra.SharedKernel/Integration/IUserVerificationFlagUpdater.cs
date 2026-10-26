using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Syncs verification flags back to the Auth user record
/// when identity verification completes in another module.
/// Implemented by the Auth module.
/// </summary>
public interface IUserVerificationFlagUpdater
{
    Task MarkGovernmentIdVerifiedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Copies the host's verification path onto the auth user so profiles,
    /// listings, and readiness copy can tell an individual from a company
    /// without reading the identity database.
    /// </summary>
    Task SetHostKindAsync(
        Guid userId,
        HostKind hostKind,
        string? legalBusinessName,
        CancellationToken ct = default);
}
