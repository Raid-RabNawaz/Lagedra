using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.VerificationAndRisk.Domain.Aggregates;
using Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence;

namespace Lagedra.Modules.VerificationAndRisk.Infrastructure.Repositories;

public sealed class RiskProfileRepository(RiskDbContext dbContext)
{
    public async Task<RiskProfile?> GetByTenantUserIdAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.RiskProfiles
            .FirstOrDefaultAsync(r => r.TenantUserId == tenantUserId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<RiskProfile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await dbContext.RiskProfiles
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public void Add(RiskProfile profile) =>
        dbContext.RiskProfiles.Add(profile);

    public void Update(RiskProfile profile) =>
        dbContext.RiskProfiles.Update(profile);
}
