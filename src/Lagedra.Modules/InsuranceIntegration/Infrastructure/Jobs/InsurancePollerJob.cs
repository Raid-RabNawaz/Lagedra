using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class InsurancePollerJob(
    InsuranceDbContext dbContext,
    ILogger<InsurancePollerJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var activeRecords = await dbContext.PolicyRecords
            .Where(r => r.State == InsuranceState.Active && r.ExpiresAt != null && r.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        var expired = 0;

        foreach (var record in activeRecords)
        {
            record.RecordUnknown();
            expired++;
            LogPolicyExpired(logger, record.Id, record.DealId);
        }

        if (expired > 0)
        {
            await dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
        }

        LogPollingComplete(logger, activeRecords.Count, expired);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Insurance policy expired: Record {RecordId} (Deal {DealId}), moved to Unknown")]
    private static partial void LogPolicyExpired(ILogger logger, Guid recordId, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Insurance polling complete: {Checked} checked, {Expired} expired")]
    private static partial void LogPollingComplete(ILogger logger, int @checked, int expired);
}
