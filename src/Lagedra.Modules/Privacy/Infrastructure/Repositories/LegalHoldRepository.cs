using Lagedra.Modules.Privacy.Domain.Entities;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Infrastructure.Repositories;

public sealed class LegalHoldRepository(PrivacyDbContext dbContext)
{
    public async Task<LegalHold?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.LegalHolds
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<LegalHold>> GetActiveByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.LegalHolds
            .Where(h => h.UserId == userId && h.ReleasedAt == null)
            .OrderByDescending(h => h.AppliedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(LegalHold legalHold) =>
        dbContext.LegalHolds.Add(legalHold);
}
