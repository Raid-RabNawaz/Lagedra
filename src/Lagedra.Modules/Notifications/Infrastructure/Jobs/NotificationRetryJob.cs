using Lagedra.Modules.Notifications.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;
using Lagedra.Modules.Notifications.Infrastructure.Repositories;
using Quartz;

namespace Lagedra.Modules.Notifications.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public sealed partial class NotificationRetryJob(
    NotificationRepository notificationRepository,
    IMediator mediator,
    ILogger<NotificationRetryJob> logger) : IJob
{
    private const int MaxAttempts = 5;
    private const int BatchSize = 50;

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var failed = await notificationRepository
            .GetFailedForRetryAsync(MaxAttempts, BatchSize, context.CancellationToken)
            .ConfigureAwait(false);

        if (failed.Count == 0)
        {
            return;
        }

        LogRetryBatchStarted(logger, failed.Count);

        var retried = 0;
        foreach (var notification in failed)
        {
            await mediator.Send(
                new SendEmailNotificationCommand(notification.Id),
                context.CancellationToken).ConfigureAwait(false);
            retried++;
        }

        LogRetryBatchCompleted(logger, retried);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification retry job started: {Count} failed notifications to retry")]
    private static partial void LogRetryBatchStarted(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Notification retry job completed: {Retried} notifications retried")]
    private static partial void LogRetryBatchCompleted(ILogger logger, int retried);
}
