using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.Privacy.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class DataExportPurgeJob(
    PrivacyDbContext dbContext,
    ILogger<DataExportPurgeJob> logger) : IJob
{
    private static readonly TimeSpan ExportPackageRetention = TimeSpan.FromDays(7);

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cutoff = DateTime.UtcNow - ExportPackageRetention;

        var expiredExports = await dbContext.DataExportRequests
            .Where(e => e.Status == ExportStatus.Completed && e.CompletedAt < cutoff)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        foreach (var export in expiredExports)
        {
            dbContext.DataExportRequests.Remove(export);
        }

        if (expiredExports.Count > 0)
        {
            await dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
        }

        LogPurgeComplete(logger, expiredExports.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Data export purge complete: {PurgedCount} expired exports removed")]
    private static partial void LogPurgeComplete(ILogger logger, int purgedCount);
}
