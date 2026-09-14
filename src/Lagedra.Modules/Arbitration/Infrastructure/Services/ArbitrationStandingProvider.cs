using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Infrastructure.Services;

public sealed class ArbitrationStandingProvider(ArbitrationDbContext dbContext) : IArbitrationStandingProvider
{
    public async Task<bool> HasUnresolvedCaseAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.ArbitrationCases
            .AsNoTracking()
            .AnyAsync(
                c => c.FiledByUserId == userId
                     && c.Status != ArbitrationStatus.Decided
                     && c.Status != ArbitrationStatus.Closed,
                ct)
            .ConfigureAwait(false);
    }

    public async Task<bool> HasPlatformBanAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.DecisionPenalties
            .AsNoTracking()
            .AnyAsync(
                p => p.PartyUserId == userId && p.PenaltyType == PenaltyType.PlatformBan,
                ct)
            .ConfigureAwait(false);
    }
}
