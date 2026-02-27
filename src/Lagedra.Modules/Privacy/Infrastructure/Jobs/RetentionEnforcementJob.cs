using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.Modules.Privacy.Domain.ValueObjects;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.Privacy.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class RetentionEnforcementJob(
    PrivacyDbContext dbContext,
    ILogger<RetentionEnforcementJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var staleDeletionCutoff = DateTime.UtcNow.AddDays(-RetentionPeriod.CancelledPreActivationDays);

        var staleDeletions = await dbContext.DeletionRequests
            .Where(d => d.Status == DeletionStatus.Requested && d.RequestedAt < staleDeletionCutoff)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        foreach (var deletion in staleDeletions)
        {
            deletion.Complete();
        }

        if (staleDeletions.Count > 0)
        {
            await dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
        }

        LogRetentionComplete(logger, staleDeletions.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Retention enforcement complete: {ProcessedCount} stale deletion requests auto-completed")]
    private static partial void LogRetentionComplete(ILogger logger, int processedCount);
}
