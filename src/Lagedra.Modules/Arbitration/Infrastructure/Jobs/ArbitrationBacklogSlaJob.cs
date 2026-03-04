using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.Arbitration.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class ArbitrationBacklogSlaJob(
    ArbitrationDbContext dbContext,
    ILogger<ArbitrationBacklogSlaJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var now = DateTime.UtcNow;

        var overdueCases = await dbContext.ArbitrationCases
            .Where(c => c.Status == ArbitrationStatus.EvidenceComplete
                        || c.Status == ArbitrationStatus.UnderReview)
            .Where(c => c.DecisionDueAt != null && c.DecisionDueAt < now)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (overdueCases.Count == 0)
        {
            LogNoOverdueCases(logger);
            return;
        }

        LogOverdueCasesDetected(logger, overdueCases.Count);

        foreach (var c in overdueCases)
        {
            LogOverdueCaseDetail(logger, c.Id, c.DealId, c.DecisionDueAt!.Value);
        }

        overdueCases[0].RaiseBacklogEscalation(overdueCases.Count);
        await dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        LogEscalationRaised(logger, overdueCases.Count);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Arbitration backlog SLA check: no overdue cases")]
    private static partial void LogNoOverdueCases(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Arbitration backlog SLA check: {Count} overdue cases detected")]
    private static partial void LogOverdueCasesDetected(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Overdue case {CaseId} (Deal {DealId}), decision was due {DueAt}")]
    private static partial void LogOverdueCaseDetail(ILogger logger, Guid caseId, Guid dealId, DateTime dueAt);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Backlog escalation raised for {Count} overdue cases")]
    private static partial void LogEscalationRaised(ILogger logger, int count);
}
