using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Services;

public sealed class PartnerEndorsementProvider(
    PartnerDbContext dbContext,
    IClock clock)
    : IPartnerEndorsementProvider
{
    public async Task<bool> HasActiveEndorsementAsync(Guid tenantUserId, CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        return await dbContext.Endorsements
            .AsNoTracking()
            .AnyAsync(e => e.TenantUserId == tenantUserId
                        && e.Status == PartnerEndorsementStatus.Approved
                        && (e.ExpiresAt == null || e.ExpiresAt > now), ct)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ActiveEndorsementInfo>> GetActiveEndorsementsAsync(
        Guid tenantUserId,
        CancellationToken ct = default)
    {
        var now = clock.UtcNow;
        var rows = await dbContext.Endorsements
            .AsNoTracking()
            .Where(e => e.TenantUserId == tenantUserId
                     && e.Status == PartnerEndorsementStatus.Approved
                     && (e.ExpiresAt == null || e.ExpiresAt > now))
            .Join(dbContext.Organizations.AsNoTracking(),
                e => e.OrganizationId,
                o => o.Id,
                (e, o) => new ActiveEndorsementInfo(
                    e.Id,
                    e.OrganizationId,
                    o.Name,
                    e.ApprovedAt!.Value,
                    e.ExpiresAt ?? DateTime.MaxValue))
            .OrderByDescending(x => x.ApprovedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.AsReadOnly();
    }
}
