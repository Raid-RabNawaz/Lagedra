using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Repositories;

public sealed class FraudFlagRepository(IntegrityDbContext dbContext)
{
    public async Task<IReadOnlyList<FraudFlag>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.FraudFlags
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.FlaggedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(FraudFlag flag) =>
        dbContext.FraudFlags.Add(flag);
}
