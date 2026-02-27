using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class FraudFlagSlaMonitorJob(
    IdentityDbContext dbContext,
    ILogger<FraudFlagSlaMonitorJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var now = DateTime.UtcNow;

        var overdueFlags = await dbContext.FraudFlags
            .Where(f => f.ResolvedAt == null && !f.IsEscalated && f.SlaDeadline < now)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (overdueFlags.Count == 0)
        {
            LogNoOverdueFlags(logger);
            return;
        }

        foreach (var flag in overdueFlags)
        {
            flag.Escalate();
            LogFlagEscalated(logger, flag.Id, flag.UserId, flag.SlaDeadline);
        }

        await dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        LogSlaMonitorComplete(logger, overdueFlags.Count);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No overdue fraud flags found")]
    private static partial void LogNoOverdueFlags(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Fraud flag {FlagId} for user {UserId} escalated — SLA deadline was {SlaDeadline}")]
    private static partial void LogFlagEscalated(ILogger logger, Guid flagId, Guid userId, DateTime slaDeadline);

    [LoggerMessage(Level = LogLevel.Information, Message = "Fraud flag SLA monitor complete: {EscalatedCount} flags escalated")]
    private static partial void LogSlaMonitorComplete(ILogger logger, int escalatedCount);
}
