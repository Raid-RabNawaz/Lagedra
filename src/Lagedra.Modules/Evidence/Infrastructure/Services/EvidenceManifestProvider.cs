using Lagedra.Modules.Evidence.Domain.Enums;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Infrastructure.Services;

public sealed class EvidenceManifestProvider(EvidenceDbContext dbContext) : IEvidenceManifestProvider
{
    public async Task<bool> ExistsAndIsSealedAsync(Guid manifestId, CancellationToken ct = default)
    {
        return await dbContext.Manifests
            .AsNoTracking()
            .AnyAsync(m => m.Id == manifestId && m.Status == ManifestStatus.Sealed, ct)
            .ConfigureAwait(false);
    }
}
