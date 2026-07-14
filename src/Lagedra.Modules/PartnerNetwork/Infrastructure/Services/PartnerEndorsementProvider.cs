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

        // Phase 17 — the previous implementation projected directly into
        // the ActiveEndorsementInfo record using null-forgiving operators
        // (e.ApprovedAt!.Value) and DateTime.MaxValue coalesce, both of
        // which Npgsql refuses to translate. Pull the raw nullable shape
        // from the DB first, then project in memory after the round-trip.
        var rows = await dbContext.Endorsements
            .AsNoTracking()
            .Where(e => e.TenantUserId == tenantUserId
                     && e.Status == PartnerEndorsementStatus.Approved
                     && (e.ExpiresAt == null || e.ExpiresAt > now))
            .Join(dbContext.Organizations.AsNoTracking(),
                e => e.OrganizationId,
                o => o.Id,
                (e, o) => new
                {
                    e.Id,
                    e.OrganizationId,
                    OrganizationName = o.Name,
                    e.ApprovedAt,
                    e.ExpiresAt,
                })
            .OrderByDescending(x => x.ApprovedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows
            .Select(r => new ActiveEndorsementInfo(
                r.Id,
                r.OrganizationId,
                r.OrganizationName,
                r.ApprovedAt ?? DateTime.MinValue,
                r.ExpiresAt ?? DateTime.MaxValue))
            .ToList()
            .AsReadOnly();
    }

    public async Task<Guid?> GetReviewEligibleEndorsementIdAsync(
        Guid tenantUserId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        // Any endorsement that reached Approved (including later Revoked/Expired).
        var id = await dbContext.Endorsements
            .AsNoTracking()
            .Where(e => e.TenantUserId == tenantUserId
                     && e.OrganizationId == organizationId
                     && (e.Status == PartnerEndorsementStatus.Approved
                         || e.Status == PartnerEndorsementStatus.Revoked
                         || e.Status == PartnerEndorsementStatus.Expired
                         || e.ApprovedAt != null))
            .OrderByDescending(e => e.ApprovedAt ?? e.CreatedAt)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return id;
    }
}
