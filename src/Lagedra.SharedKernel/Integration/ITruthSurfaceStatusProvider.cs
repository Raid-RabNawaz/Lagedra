namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module provider for truth surface snapshot status.
/// Implemented by TruthSurface, consumed by ActivationAndBilling.
/// </summary>
public interface ITruthSurfaceStatusProvider
{
    /// <summary>
    /// Returns the truth surface status for each deal that has a snapshot.
    /// Deals without snapshots are omitted from the result.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, TruthSurfaceSnapshotInfo>> GetStatusesForDealsAsync(
        IReadOnlyList<Guid> dealIds,
        CancellationToken ct = default);
}

public sealed record TruthSurfaceSnapshotInfo(
    Guid SnapshotId,
    bool IsSealed);
