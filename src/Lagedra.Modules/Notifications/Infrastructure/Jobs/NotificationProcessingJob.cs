using Lagedra.Modules.Notifications.Application.Commands;
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

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var ct = context.CancellationToken;

        var queued = await dbContext.Notifications
            .Where(n => n.Status == NotificationStatus.Queued
                && n.ScheduledAt <= DateTime.UtcNow)
            .OrderBy(n => n.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (queued.Count == 0) return;

        LogProcessing(queued.Count);

        foreach (var notification in queued)
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
            catch (InvalidOperationException ex)
            {
                LogDeliveryFailed(notification.Id, ex.Message);
            }
            catch (TimeoutException ex)
            {
                LogDeliveryFailed(notification.Id, ex.Message);
            }
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Processing {Count} queued notifications")]
    private partial void LogProcessing(int count);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to deliver notification {NotificationId}: {Error}")]
    private partial void LogDeliveryFailed(Guid notificationId, string error);
}
