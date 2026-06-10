namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Lets the Evidence module authorize arbitrators assigned to a case that references a manifest.
/// </summary>
public interface IArbitrationEvidenceManifestAccessProvider
{
    Task<bool> IsAssignedArbitratorForManifestAsync(
        Guid arbitratorUserId,
        Guid evidenceManifestId,
        CancellationToken cancellationToken = default);
}
