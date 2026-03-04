using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Domain.Events;
using Lagedra.Modules.InsuranceIntegration.Domain.Policies;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class InsuranceUnknownSlaJob(
    InsuranceDbContext dbContext,
    ILogger<InsuranceUnknownSlaJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var utcNow = DateTime.UtcNow;

        var unknownRecords = await dbContext.PolicyRecords
            .Where(r => r.State == InsuranceState.Unknown && r.UnknownSince != null)
            .ToListAsync(context.CancellationToken)
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

        if (breached > 0)
        {
            await dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);
        }

        LogSlaCheckComplete(logger, unknownRecords.Count, breached);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "72h SLA breached: Record {RecordId} (Deal {DealId}), unknown since {UnknownSince}. Marking lapsed.")]
    private static partial void LogSlaBreached(ILogger logger, Guid recordId, Guid dealId, DateTime unknownSince);

    [LoggerMessage(Level = LogLevel.Information, Message = "Unknown SLA check complete: {Total} checked, {Breached} breached")]
    private static partial void LogSlaCheckComplete(ILogger logger, int total, int breached);
}
