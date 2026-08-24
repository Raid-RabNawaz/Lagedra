using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Aggregates;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.Notifications.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class NotificationProcessingJob(
    NotificationDbContext dbContext,
    IMediator mediator,
    ILogger<NotificationProcessingJob> logger) : IJob
{
    private const int BatchSize = 100;
    private const int MaxAttempts = 5;

    /// <summary>
    /// Claims the oldest due notification with a row-level lock, exactly like
    /// the outbox processor. The previous implementation read the whole batch
    /// without locking, so any concurrent execution (a second scheduler
    /// instance, or overlap during a rolling deploy) read the same Queued
    /// rows and delivered them twice — duplicate emails and bell entries.
    /// SKIP LOCKED guarantees each row is claimed by exactly one processor.
    ///
    /// Retries are folded into the same claim (this replaced the separate
    /// NotificationRetryJob): a Failed row becomes claimable again 10 minutes
    /// after its last attempt (UpdatedAt refreshes on every save) until it
    /// exhausts MaxAttempts, giving delivery a built-in backoff without a
    /// second scheduled job.
    ///
    /// The statement is fully raw so EF doesn't wrap it in a subquery (which
    /// would strip the locking clause); column identifiers are the entity's
    /// PascalCase names as stored by Postgres.
    /// </summary>
    private static readonly string ClaimSql =
        "SELECT * FROM notifications.notifications "
        + "WHERE ((\"Status\" = 'Queued' AND \"ScheduledAt\" <= now()) "
        + $"OR (\"Status\" = 'Failed' AND \"AttemptCount\" < {MaxAttempts} "
        + "AND \"UpdatedAt\" <= now() - interval '10 minutes')) "
        + "ORDER BY \"CreatedAt\" "
        + "LIMIT 1 "
        + "FOR UPDATE SKIP LOCKED";

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;

        var processed = 0;
        while (processed < BatchSize && !ct.IsCancellationRequested)
        {
            var claimedOne = await ClaimAndDeliverOneAsync(ct).ConfigureAwait(false);
            if (!claimedOne)
            {
                break;
            }

            processed++;
        }

        if (processed > 0)
        {
            LogProcessing(processed);
        }
    }

    /// <summary>
    /// Opens a transaction, claims one row, delivers it, and commits. The
    /// claim + status change + commit are one atomic unit, so a concurrent
    /// processor skips the locked row instead of re-delivering it, and a
    /// crash mid-delivery only re-runs the single in-flight notification.
    /// </summary>
    private async Task<bool> ClaimAndDeliverOneAsync(CancellationToken ct)
    {
        var transaction = await dbContext.Database
            .BeginTransactionAsync(ct).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false))
        {
            var claimed = await dbContext.Notifications
                .FromSqlRaw(ClaimSql)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (claimed.Count == 0)
            {
                await transaction.CommitAsync(ct).ConfigureAwait(false);
                return false;
            }

            await DeliverAsync(claimed[0], ct).ConfigureAwait(false);

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
            await transaction.CommitAsync(ct).ConfigureAwait(false);
            return true;
        }
    }

    private async Task DeliverAsync(Notification notification, CancellationToken ct)
    {
        try
        {
            switch (notification.Channel)
            {
                case NotificationChannel.Email:
                    await mediator
                        .Send(new SendEmailNotificationCommand(notification.Id), ct)
                        .ConfigureAwait(false);
                    break;

                case NotificationChannel.Sms:
                    await mediator
                        .Send(new SendSmsNotificationCommand(notification.Id), ct)
                        .ConfigureAwait(false);
                    break;

                case NotificationChannel.InApp:
                    notification.Payload.TryGetValue("title", out var title);
                    notification.Payload.TryGetValue("body", out var body);
                    notification.Payload.TryGetValue("relatedEntityType", out var entityType);

                    Guid? entityId = notification.Payload.TryGetValue("relatedEntityId", out var eid)
                        && Guid.TryParse(eid, out var parsed)
                        ? parsed : null;

                    await mediator.Send(new DeliverInAppNotificationCommand(
                        notification.RecipientUserId,
                        title ?? notification.TemplateId,
                        body ?? string.Empty,
                        notification.TemplateId,
                        entityId,
                        entityType), ct).ConfigureAwait(false);

                    notification.MarkSent(DateTime.UtcNow);
                    break;
            }
        }
#pragma warning disable CA1031 // a delivery failure must not abort the batch
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // The row must leave the claimable pool: MarkFailed bumps
            // AttemptCount and refreshes UpdatedAt, so it only becomes
            // claimable again after the retry delay (or is poisoned once
            // MaxAttempts is reached). Without this the claim loop would
            // immediately re-select it and spin for the whole tick.
            if (notification.Status is NotificationStatus.Queued or NotificationStatus.Failed)
            {
                notification.MarkFailed(ex.Message);
            }

            LogDeliveryFailed(notification.Id, ex.Message);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Processing {Count} queued notifications")]
    private partial void LogProcessing(int count);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to deliver notification {NotificationId}: {Error}")]
    private partial void LogDeliveryFailed(Guid notificationId, string error);
}
