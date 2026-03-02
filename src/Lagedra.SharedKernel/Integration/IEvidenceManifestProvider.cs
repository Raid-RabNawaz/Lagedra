namespace Lagedra.SharedKernel.Integration;

public interface IEvidenceManifestProvider
{
    Task<bool> ExistsAndIsSealedAsync(Guid manifestId, CancellationToken ct = default);
}
