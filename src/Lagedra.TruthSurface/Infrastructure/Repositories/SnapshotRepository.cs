using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Infrastructure.Repositories;

public sealed class SnapshotRepository(TruthSurfaceDbContext dbContext)
{
    public async Task<TruthSnapshot?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.Snapshots
            .Include(s => s.Proof)
            .FirstOrDefaultAsync(s => s.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<TruthSnapshot?> GetActiveForDealAsync(Guid dealId, CancellationToken ct = default) =>
        await dbContext.Snapshots
            .Include(s => s.Proof)
            .Where(s => s.DealId == dealId && s.Status == TruthSurfaceStatus.Confirmed)
            .OrderByDescending(s => s.SealedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<TruthSnapshot>> GetAllForDealAsync(Guid dealId, CancellationToken ct = default) =>
        await dbContext.Snapshots
            .AsNoTracking()
            .Include(s => s.Proof)
            .Where(s => s.DealId == dealId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public void Add(TruthSnapshot snapshot) =>
        dbContext.Snapshots.Add(snapshot);
}
