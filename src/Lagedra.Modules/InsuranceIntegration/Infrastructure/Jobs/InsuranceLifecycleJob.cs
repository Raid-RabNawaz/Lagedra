using Lagedra.SharedKernel.Integration;
using Lagedra.Modules.InsuranceIntegration.Domain.Policies;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Jobs;

/// <summary>
/// Single sweep over the policy lifecycle, replacing the former
/// InsurancePollerJob (hourly) and InsuranceUnknownSlaJob (every 30 min):
/// first move expired Active policies to Unknown, then mark Unknown policies
/// Lapsed once their grace window is breached. Doing both in one pass means a
/// policy that expires can be lapsed on the very next run instead of waiting
/// for two independently scheduled jobs to line up.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class InsuranceLifecycleJob(
    InsuranceDbContext dbContext,
    ILogger<InsuranceLifecycleJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;
        var utcNow = DateTime.UtcNow;

        // Step 1 — expired Active policies become Unknown.
        var activeRecords = await dbContext.PolicyRecords
            .Where(r => r.State == InsuranceState.Active && r.ExpiresAt != null && r.ExpiresAt <= utcNow)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var expired = 0;
        foreach (var record in activeRecords)
        {
            record.RecordUnknown();
            expired++;
            LogPolicyExpired(logger, record.Id, record.DealId);
        }

        // Step 2 — Unknown policies past their grace window become Lapsed.
        var unknownRecords = await dbContext.PolicyRecords
            .Where(r => r.State == InsuranceState.Unknown && r.UnknownSince != null)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var breached = 0;
        foreach (var record in unknownRecords)
        {
            if (!UnknownGraceWindowPolicy.IsBreached(record.UnknownSince!.Value, utcNow))
            {
                continue;
            }

            record.MarkLapsed();
            breached++;
            LogSlaBreached(logger, record.Id, record.DealId, record.UnknownSince!.Value);
        }

        if (expired > 0 || breached > 0)
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        LogLifecycleComplete(logger, activeRecords.Count, expired, unknownRecords.Count, breached);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Insurance policy expired: Record {RecordId} (Deal {DealId}), moved to Unknown")]
    private static partial void LogPolicyExpired(ILogger logger, Guid recordId, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "72h SLA breached: Record {RecordId} (Deal {DealId}), unknown since {UnknownSince}. Marking lapsed.")]
    private static partial void LogSlaBreached(ILogger logger, Guid recordId, Guid dealId, DateTime unknownSince);

    [LoggerMessage(Level = LogLevel.Information, Message = "Insurance lifecycle sweep complete: {ActiveChecked} active checked, {Expired} expired; {UnknownChecked} unknown checked, {Breached} lapsed")]
    private static partial void LogLifecycleComplete(ILogger logger, int activeChecked, int expired, int unknownChecked, int breached);
}
