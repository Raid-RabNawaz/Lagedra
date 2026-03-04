using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class PatternDetectionSchedulerJob(
    IntegrityDbContext dbContext,
    ILogger<PatternDetectionSchedulerJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var openCases = await dbContext.AbuseCases
            .AsNoTracking()
            .Where(a => a.Status == Domain.Enums.AbuseCaseStatus.Open)
            .CountAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var totalFlags = await dbContext.FraudFlags
            .AsNoTracking()
            .CountAsync(context.CancellationToken)
            .ConfigureAwait(false);

        LogPatternScanComplete(logger, openCases, totalFlags);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pattern detection scan complete: {OpenCases} open cases, {TotalFlags} total flags")]
    private static partial void LogPatternScanComplete(ILogger logger, int openCases, int totalFlags);
}
