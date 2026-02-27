using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record SendEmailNotificationCommand(Guid NotificationId) : IRequest<Result>;

public sealed partial class SendEmailNotificationCommandHandler(
    NotificationDbContext dbContext,
    IEmailService emailService,
    ILogger<SendEmailNotificationCommandHandler> logger)
    : IRequestHandler<SendEmailNotificationCommand, Result>
{
    public async Task<Result> Handle(
        SendEmailNotificationCommand request,
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
                                      && t.Channel == NotificationChannel.Email, cancellationToken)
            .ConfigureAwait(false);

        if (template is null)
        {
            notification.MarkFailed($"Template '{notification.TemplateId}' not found for Email channel.");
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result.Failure(new Error("Notification.TemplateNotFound", "Email template not found."));
        }

        try
        {
            var emailMessage = new EmailMessage
            {
                To = notification.RecipientEmail,
                Subject = template.RenderSubject(notification.Payload),
                HtmlBody = template.RenderHtmlBody(notification.Payload),
                PlainTextBody = template.RenderPlainTextBody(notification.Payload)
            };

            await emailService.SendAsync(emailMessage, cancellationToken).ConfigureAwait(false);

            notification.MarkSent(DateTime.UtcNow);

            dbContext.DeliveryLogs.Add(new DeliveryLog(
                notification.Id, brevoMessageId: null, deliveredAt: null, error: null));

            LogEmailSent(logger, notification.Id, notification.RecipientEmail);
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            notification.MarkFailed(ex.Message);

            dbContext.DeliveryLogs.Add(new DeliveryLog(
                notification.Id, brevoMessageId: null, deliveredAt: null, error: ex.Message));

            LogEmailFailed(logger, notification.Id, notification.RecipientEmail, ex);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return notification.Status == NotificationStatus.Sent
            ? Result.Success()
            : Result.Failure(new Error("Notification.SendFailed", notification.LastError ?? "Send failed."));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Email sent for notification {NotificationId} to {RecipientEmail}")]
    private static partial void LogEmailSent(ILogger logger, Guid notificationId, string recipientEmail);

    [LoggerMessage(Level = LogLevel.Error, Message = "Email send failed for notification {NotificationId} to {RecipientEmail}")]
    private static partial void LogEmailFailed(ILogger logger, Guid notificationId, string recipientEmail, Exception ex);
}
