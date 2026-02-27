using Lagedra.Compliance.Domain;
using Lagedra.Compliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Infrastructure.Repositories;

public sealed class ViolationRepository(ComplianceDbContext dbContext)
{
    public async Task<Violation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.Violations
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Violation>> GetByDealAsync(Guid dealId, CancellationToken ct = default) =>
        await dbContext.Violations
            .AsNoTracking()
            .Where(v => v.DealId == dealId)
            .OrderByDescending(v => v.DetectedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public void Add(Violation violation) =>
        dbContext.Violations.Add(violation);
}
