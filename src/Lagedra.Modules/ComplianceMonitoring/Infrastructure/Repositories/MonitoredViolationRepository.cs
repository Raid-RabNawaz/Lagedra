using Lagedra.Modules.ComplianceMonitoring.Domain.Entities;
using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;
using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ComplianceMonitoring.Infrastructure.Repositories;

public sealed class MonitoredViolationRepository(ComplianceMonitoringDbContext dbContext)
{
    public async Task<MonitoredViolation?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Violations
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MonitoredViolation>> GetOpenByDealAsync(
        Guid dealId, CancellationToken cancellationToken = default) =>
        await dbContext.Violations
            .Where(v => v.DealId == dealId && v.Status == MonitoredViolationStatus.Open)
            .OrderByDescending(v => v.DetectedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<MonitoredViolation>> GetByDealAsync(
        Guid dealId, CancellationToken cancellationToken = default) =>
        await dbContext.Violations
            .AsNoTracking()
            .Where(v => v.DealId == dealId)
            .OrderByDescending(v => v.DetectedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(MonitoredViolation violation) =>
        dbContext.Violations.Add(violation);
}
