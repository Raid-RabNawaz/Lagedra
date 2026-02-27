using Lagedra.Modules.Arbitration.Domain.Aggregates;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Infrastructure.Repositories;

public sealed class ArbitrationCaseRepository(ArbitrationDbContext dbContext)
{
    public async Task<ArbitrationCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.ArbitrationCases
            .Include(c => c.EvidenceSlots)
            .Include(c => c.ArbitratorAssignments)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ArbitrationCase>> GetByStatusAsync(
        ArbitrationStatus status,
        CancellationToken cancellationToken = default) =>
        await dbContext.ArbitrationCases
            .AsNoTracking()
            .Include(c => c.EvidenceSlots)
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.FiledAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<ArbitrationCase>> GetByArbitratorUserIdAsync(
        Guid arbitratorUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ArbitrationCases
            .AsNoTracking()
            .Include(c => c.EvidenceSlots)
            .Include(c => c.ArbitratorAssignments)
            .Where(c => c.ArbitratorAssignments.Any(a => a.ArbitratorUserId == arbitratorUserId))
            .OrderByDescending(c => c.FiledAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(ArbitrationCase arbitrationCase) =>
        dbContext.ArbitrationCases.Add(arbitrationCase);
}
