using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Services;

public sealed class VerificationSignalProvider(IdentityDbContext dbContext) : IVerificationSignalProvider
{
    public async Task<VerificationSignalDto?> GetSignalsAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await dbContext.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return null;
        }

        var latestBgCheck = await dbContext.BackgroundCheckReports
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReceivedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return new VerificationSignalDto(
            IsIdentityVerified: profile.Status == VerificationStatus.Verified,
            IsIdentityPending: profile.Status == VerificationStatus.Pending,
            IsIdentityFailed: profile.Status == VerificationStatus.Failed,
            IsBackgroundCheckPassed: latestBgCheck?.Result == BackgroundCheckResult.Pass,
            IsBackgroundCheckFailed: latestBgCheck?.Result == BackgroundCheckResult.Fail,
            IsBackgroundCheckUnderReview: latestBgCheck?.Result == BackgroundCheckResult.Review,
            LastScreeningAt: latestBgCheck?.ReceivedAt);
    }

    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetLatestScreeningTimesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, DateTime>();
        }

        var rows = await dbContext.BackgroundCheckReports
            .AsNoTracking()
            .Where(r => userIds.Contains(r.UserId))
            .GroupBy(r => r.UserId)
            .Select(g => new { UserId = g.Key, At = g.Max(r => r.ReceivedAt) })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.ToDictionary(r => r.UserId, r => r.At);
    }

    public async Task<IReadOnlyDictionary<Guid, ApplicationScreeningOrderDto>> GetLatestScreeningOrdersAsync(
        IReadOnlyCollection<Guid> applicationIds,
        CancellationToken ct = default)
    {
        if (applicationIds.Count == 0)
        {
            return new Dictionary<Guid, ApplicationScreeningOrderDto>();
        }

        var orders = await dbContext.ScreeningOrders
            .AsNoTracking()
            .Where(o => applicationIds.Contains(o.ApplicationId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var latest = orders
            .GroupBy(o => o.ApplicationId)
            .Select(g => g.OrderByDescending(o => o.CreatedAt).First())
            .ToList();

        if (latest.Count == 0)
        {
            return new Dictionary<Guid, ApplicationScreeningOrderDto>();
        }

        var tenantIds = latest.Select(o => o.TenantUserId).Distinct().ToList();
        var reports = await dbContext.BackgroundCheckReports
            .AsNoTracking()
            .Where(r => tenantIds.Contains(r.UserId))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var result = new Dictionary<Guid, ApplicationScreeningOrderDto>(latest.Count);
        foreach (var order in latest)
        {
            string? outcome = null;
            if (order.Status == ScreeningOrderStatus.Completed)
            {
                var report = reports
                    .Where(r => r.UserId == order.TenantUserId && r.ReceivedAt >= order.CreatedAt)
                    .OrderByDescending(r => r.ReceivedAt)
                    .FirstOrDefault();
                outcome = report?.Result.ToString();
            }

            result[order.ApplicationId] = new ApplicationScreeningOrderDto(
                order.ApplicationId,
                order.HostUserId,
                order.Status.ToString(),
                outcome);
        }

        return result;
    }
}
