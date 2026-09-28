using Lagedra.Modules.LeaseAgreements.Application.Services;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;
using Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Services;

public sealed class DealLeaseSignatureStore(LeaseAgreementDbContext db) : IDealLeaseSignatureStore
{
    public async Task<IReadOnlyList<DealLeaseSignature>> ListByDealAsync(
        Guid dealId,
        CancellationToken ct = default)
    {
        return await db.Signatures.AsNoTracking()
            .Where(s => s.DealId == dealId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<DealLeaseSignature?> GetAsync(
        Guid dealId,
        DealLeaseSignerRole role,
        CancellationToken ct = default)
    {
        return await db.Signatures.AsNoTracking()
            .FirstOrDefaultAsync(s => s.DealId == dealId && s.Role == role, ct)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(DealLeaseSignature signature, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(signature);

        db.Signatures.Add(signature);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
