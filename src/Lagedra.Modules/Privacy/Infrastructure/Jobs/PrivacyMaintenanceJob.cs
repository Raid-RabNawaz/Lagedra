using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.Modules.Privacy.Domain.ValueObjects;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.Privacy.Infrastructure.Jobs;

/// <summary>
/// Nightly privacy housekeeping, replacing the former RetentionEnforcementJob
/// (01:00) and DataExportPurgeJob (05:00): auto-completes stale deletion
/// requests and purges completed data-export packages past their retention.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class PrivacyMaintenanceJob(
    PrivacyDbContext dbContext,
    ILogger<PrivacyMaintenanceJob> logger) : IJob
{
    private static readonly TimeSpan ExportPackageRetention = TimeSpan.FromDays(7);

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;
        var utcNow = DateTime.UtcNow;

        // Step 1 — auto-complete deletion requests stuck in Requested.
        var staleDeletionCutoff = utcNow.AddDays(-RetentionPeriod.CancelledPreActivationDays);

        var staleDeletions = await dbContext.DeletionRequests
            .Where(d => d.Status == DeletionStatus.Requested && d.RequestedAt < staleDeletionCutoff)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var deletion in staleDeletions)
        {
            deletion.Complete();
        }

        // Step 2 — purge completed export packages past retention.
        var exportCutoff = utcNow - ExportPackageRetention;

        var expiredExports = await dbContext.DataExportRequests
            .Where(e => e.Status == ExportStatus.Completed && e.CompletedAt < exportCutoff)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var export in expiredExports)
        {
            dbContext.DataExportRequests.Remove(export);
        }

        if (staleDeletions.Count > 0 || expiredExports.Count > 0)
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        LogMaintenanceComplete(logger, staleDeletions.Count, expiredExports.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Privacy maintenance complete: {DeletionsCompleted} stale deletion requests auto-completed, {ExportsPurged} expired exports removed")]
    private static partial void LogMaintenanceComplete(ILogger logger, int deletionsCompleted, int exportsPurged);
}
