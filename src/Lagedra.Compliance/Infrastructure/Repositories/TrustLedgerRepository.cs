using Lagedra.Compliance.Domain;
using Lagedra.Compliance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Infrastructure.Repositories;

/// <summary>
/// Write-only append, read-only projection.
/// No update or delete methods — the trust ledger is immutable.
/// </summary>
public sealed class TrustLedgerRepository(ComplianceDbContext dbContext)
{
    public void Append(TrustLedgerEntry entry) =>
        dbContext.TrustLedgerEntries.Add(entry);

    public async Task<IReadOnlyList<TrustLedgerEntry>> GetPublicForUserAsync(Guid userId, CancellationToken ct = default) =>
        await dbContext.TrustLedgerEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.IsPublic)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<TrustLedgerEntry>> GetAllForDealAsync(Guid dealId, CancellationToken ct = default) =>
        await dbContext.TrustLedgerEntries
            .AsNoTracking()
            .Where(e => e.ReferenceId == dealId)
            .OrderByDescending(e => e.OccurredAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
