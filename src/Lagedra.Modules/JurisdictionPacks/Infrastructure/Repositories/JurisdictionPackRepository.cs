using Lagedra.Modules.JurisdictionPacks.Domain.Aggregates;
using Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;
using Lagedra.SharedKernel.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Repositories;

public sealed class JurisdictionPackRepository(JurisdictionDbContext dbContext)
{
    public IUnitOfWork UnitOfWork => dbContext;

    public async Task<JurisdictionPack?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.JurisdictionPacks
            .Include(p => p.Versions)
                .ThenInclude(v => v.EffectiveDateRules)
            .Include(p => p.Versions)
                .ThenInclude(v => v.FieldGatingRules)
            .Include(p => p.Versions)
                .ThenInclude(v => v.EvidenceSchedules)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<JurisdictionPack?> GetByCodeAsync(string jurisdictionCode, CancellationToken cancellationToken = default) =>
        await dbContext.JurisdictionPacks
            .Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.JurisdictionCode.Code == jurisdictionCode, cancellationToken)
            .ConfigureAwait(false);

    public void Add(JurisdictionPack pack) =>
        dbContext.JurisdictionPacks.Add(pack);

    public void Update(JurisdictionPack pack) =>
        dbContext.JurisdictionPacks.Update(pack);
}
