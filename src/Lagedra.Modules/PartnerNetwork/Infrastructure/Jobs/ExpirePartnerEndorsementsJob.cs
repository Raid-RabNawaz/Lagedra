using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class ExpirePartnerEndorsementsJob(
    PartnerDbContext dbContext,
    IClock clock,
    IEventBus eventBus,
    ILogger<ExpirePartnerEndorsementsJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var now = clock.UtcNow;

        var due = await dbContext.Endorsements
            .Where(e => e.Status == PartnerEndorsementStatus.Approved
                     && e.ExpiresAt != null
                     && e.ExpiresAt < now)
            .Join(dbContext.Organizations,
                e => e.OrganizationId,
                o => o.Id,
                (e, o) => new { Endorsement = e, OrgName = o.Name })
            .ToListAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (due.Count == 0)
        {
            LogNoExpirations(logger);
            return;
        }

        foreach (var entry in due)
        {
            entry.Endorsement.Expire(entry.OrgName, clock);
        }

        await dbContext.SaveChangesAsync(context.CancellationToken).ConfigureAwait(false);

        // Publish integration events. Domain events on the aggregates are already added
        // by Expire(...); this loop also raises them via the event bus for cross-module
        // subscribers that don't poll the outbox.
        foreach (var entry in due)
        {
            var integrationEvent = entry.Endorsement.DomainEvents
                .OfType<SharedKernel.Integration.Events.PartnerEndorsementExpiredEvent>()
                .Last();
            await eventBus.Publish(integrationEvent, context.CancellationToken).ConfigureAwait(false);
        }

        LogExpirations(logger, due.Count);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Expired {Count} partner endorsements past their ExpiresAt deadline")]
    private static partial void LogExpirations(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "No partner endorsements due for expiration")]
    private static partial void LogNoExpirations(ILogger logger);
}
