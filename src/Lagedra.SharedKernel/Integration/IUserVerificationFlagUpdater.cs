namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Syncs verification flags back to the Auth user record
/// when identity verification completes in another module.
/// Implemented by the Auth module.
/// </summary>
public interface IUserVerificationFlagUpdater
{
    Task MarkGovernmentIdVerifiedAsync(Guid userId, CancellationToken ct = default);
}
