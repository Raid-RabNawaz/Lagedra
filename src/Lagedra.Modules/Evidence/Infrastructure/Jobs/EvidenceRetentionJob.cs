using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.Evidence.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class EvidenceRetentionJob(
    EvidenceDbContext dbContext,
    ILogger<EvidenceRetentionJob> logger) : IJob
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(365 * 7);

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var cutoff = DateTime.UtcNow - RetentionPeriod;

        var expiredManifests = await dbContext.Manifests
            .Where(m => m.SealedAt != null && m.SealedAt < cutoff)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (expiredManifests.Count == 0)
        {
            LogNoExpiredEvidence(logger);
            return;
        }

        LogRetentionSweep(logger, expiredManifests.Count, cutoff);

        foreach (var manifest in expiredManifests)
        {
            LogManifestExpired(logger, manifest.Id, manifest.DealId, manifest.SealedAt!.Value);
        }

        LogRetentionComplete(logger, expiredManifests.Count);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No expired evidence manifests found")]
    private static partial void LogNoExpiredEvidence(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retention sweep found {Count} manifests sealed before {Cutoff}")]
    private static partial void LogRetentionSweep(ILogger logger, int count, DateTime cutoff);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Manifest {ManifestId} (Deal {DealId}) sealed at {SealedAt} exceeds retention period")]
    private static partial void LogManifestExpired(ILogger logger, Guid manifestId, Guid dealId, DateTime sealedAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Evidence retention check complete: {Count} manifests flagged")]
    private static partial void LogRetentionComplete(ILogger logger, int count);
}
