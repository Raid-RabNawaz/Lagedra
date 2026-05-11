using Lagedra.SharedKernel.Integration;
using Lagedra.TruthSurface.Domain;
using Lagedra.TruthSurface.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Infrastructure.Services;

public sealed class TruthSurfaceStatusProvider(TruthSurfaceDbContext dbContext)
    : ITruthSurfaceStatusProvider
{
    public async Task<IReadOnlyDictionary<Guid, TruthSurfaceSnapshotInfo>> GetStatusesForDealsAsync(
        IReadOnlyList<Guid> dealIds,
        CancellationToken ct = default)
    {
        if (dealIds is null || dealIds.Count == 0)
        {
            return new Dictionary<Guid, TruthSurfaceSnapshotInfo>();
        }

        var rows = await dbContext.Snapshots
            .AsNoTracking()
            .Where(s => dealIds.Contains(s.DealId)
                        && s.Status != TruthSurfaceStatus.Superseded)
            .Select(s => new { s.DealId, s.Id, s.Status, s.CreatedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .GroupBy(s => s.DealId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g.OrderByDescending(s => s.CreatedAt).First();
                    return new TruthSurfaceSnapshotInfo(
                        latest.Id,
                        latest.Status == TruthSurfaceStatus.Confirmed);
                });
    }
}
