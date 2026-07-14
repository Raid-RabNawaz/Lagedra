using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Sms;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record SendSmsNotificationCommand(Guid NotificationId) : IRequest<Result>;

public sealed partial class SendSmsNotificationCommandHandler(
    NotificationDbContext dbContext,
    ISmsService smsService,
    ILogger<SendSmsNotificationCommandHandler> logger)
    : IRequestHandler<SendSmsNotificationCommand, Result>
{
    public async Task<Result> Handle(
        SendSmsNotificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId, cancellationToken)
            .ConfigureAwait(false);

        if (notification is null)
        {
            return Result.Failure(new Error("Notification.NotFound", "Notification not found."));
        }

        var template = await dbContext.Templates
            .FirstOrDefaultAsync(t => t.TemplateId == notification.TemplateId
                                      && t.Channel == NotificationChannel.Sms, cancellationToken)
            .ConfigureAwait(false);

        if (template is null)
        {
            notification.MarkFailed($"Template '{notification.TemplateId}' not found for Sms channel.");
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure(new Error("Notification.TemplateNotFound", "SMS template not found."));
        }

        try
        {
            // SMS templates store the body in PlainTextBody (preferred) or HtmlBody.
            var body = template.RenderPlainTextBody(notification.Payload)
                ?? template.RenderHtmlBody(notification.Payload);

            var messageSid = await smsService.SendAsync(new SmsMessage
            {
                ToE164 = notification.RecipientAddress,
                Body = body
            }, cancellationToken).ConfigureAwait(false);

            notification.MarkSent(DateTime.UtcNow);

            dbContext.DeliveryLogs.Add(new DeliveryLog(
                notification.Id, providerMessageId: messageSid, deliveredAt: null, error: null));

            LogSmsSent(logger, notification.Id, notification.RecipientAddress);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            notification.MarkFailed(ex.Message);

            dbContext.DeliveryLogs.Add(new DeliveryLog(
                notification.Id, providerMessageId: null, deliveredAt: null, error: ex.Message));

            LogSmsFailed(logger, notification.Id, notification.RecipientAddress, ex);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return notification.Status == NotificationStatus.Sent
            ? Result.Success()
            : Result.Failure(new Error("Notification.SendFailed", notification.LastError ?? "Send failed."));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS sent for notification {NotificationId} to {RecipientAddress}")]
    private static partial void LogSmsSent(ILogger logger, Guid notificationId, string recipientAddress);

    [LoggerMessage(Level = LogLevel.Error, Message = "SMS send failed for notification {NotificationId} to {RecipientAddress}")]
    private static partial void LogSmsFailed(ILogger logger, Guid notificationId, string recipientAddress, Exception ex);
}
