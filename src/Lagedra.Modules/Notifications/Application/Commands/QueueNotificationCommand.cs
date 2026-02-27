using Lagedra.Modules.Notifications.Application.DTOs;
using Lagedra.Modules.Notifications.Domain.Aggregates;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record QueueNotificationCommand(
    Guid RecipientUserId,
    string RecipientEmail,
    NotificationChannel Channel,
    string TemplateId,
    Dictionary<string, string> Payload,
    DateTime? ScheduledAt = null) : IRequest<Result<NotificationDto>>;

public sealed class QueueNotificationCommandHandler(
    NotificationDbContext dbContext)
    : IRequestHandler<QueueNotificationCommand, Result<NotificationDto>>
{
    public async Task<Result<NotificationDto>> Handle(
        QueueNotificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notification = Notification.Queue(
            request.RecipientUserId,
            request.RecipientEmail,
            request.Channel,
            request.TemplateId,
            request.Payload,
            request.ScheduledAt);

        dbContext.Notifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<NotificationDto>.Success(new NotificationDto(
            notification.Id,
            notification.RecipientUserId,
            notification.RecipientEmail,
            notification.Channel,
            notification.TemplateId,
            notification.Status,
            notification.ScheduledAt,
            notification.SentAt,
            notification.AttemptCount));
    }
}
