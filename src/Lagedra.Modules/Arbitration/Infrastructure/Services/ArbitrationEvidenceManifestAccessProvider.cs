using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Infrastructure.Services;

public sealed class ArbitrationEvidenceManifestAccessProvider(ArbitrationDbContext dbContext)
    : IArbitrationEvidenceManifestAccessProvider
{
    public async Task<bool> IsAssignedArbitratorForManifestAsync(
        Guid arbitratorUserId,
        Guid evidenceManifestId,
        CancellationToken cancellationToken = default) =>
        await dbContext.EvidenceSlots
            .AsNoTracking()
            .Where(s => s.EvidenceManifestId == evidenceManifestId)
            .Join(
                dbContext.ArbitratorAssignments.AsNoTracking(),
                slot => slot.CaseId,
                assignment => assignment.CaseId,
                (_, assignment) => assignment)
            .AnyAsync(a => a.ArbitratorUserId == arbitratorUserId, cancellationToken)
            .ConfigureAwait(false);
}
