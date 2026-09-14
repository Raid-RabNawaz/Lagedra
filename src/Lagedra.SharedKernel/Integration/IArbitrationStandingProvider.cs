namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Whether a user has an open (not decided/closed) arbitration case.
/// Implemented by Arbitration.
/// </summary>
public interface IArbitrationStandingProvider
{
    Task<bool> HasUnresolvedCaseAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// True when a decided arbitration case issued <c>PlatformBan</c> against the user.
    /// </summary>
    Task<bool> HasPlatformBanAsync(Guid userId, CancellationToken ct = default);
}
