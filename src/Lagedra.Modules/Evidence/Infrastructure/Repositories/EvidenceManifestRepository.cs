using Lagedra.Modules.Evidence.Domain.Aggregates;
using Lagedra.Modules.Evidence.Domain.Enums;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Infrastructure.Repositories;

public sealed class EvidenceManifestRepository(EvidenceDbContext dbContext)
{
    public async Task<EvidenceManifest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Manifests
            .Include(m => m.Uploads)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<EvidenceManifest>> GetByDealIdAsync(Guid dealId, CancellationToken cancellationToken = default) =>
        await dbContext.Manifests
            .AsNoTracking()
            .Include(m => m.Uploads)
            .Where(m => m.DealId == dealId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<EvidenceManifest>> GetSealedByDealIdAsync(Guid dealId, CancellationToken cancellationToken = default) =>
        await dbContext.Manifests
            .AsNoTracking()
            .Include(m => m.Uploads)
            .Where(m => m.DealId == dealId && m.Status == ManifestStatus.Sealed)
            .OrderByDescending(m => m.SealedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(EvidenceManifest manifest) =>
        dbContext.Manifests.Add(manifest);
}
