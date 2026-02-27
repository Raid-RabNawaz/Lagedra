using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.RealTime;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record DeliverInAppNotificationCommand(
    Guid RecipientUserId,
    string Title,
    string Body,
    string Category,
    Guid? RelatedEntityId = null,
    string? RelatedEntityType = null) : IRequest<Result>;

public sealed class DeliverInAppNotificationCommandHandler(
    NotificationDbContext dbContext,
    INotificationPusher pusher)
    : IRequestHandler<DeliverInAppNotificationCommand, Result>
{
    public async Task<Result> Handle(
        DeliverInAppNotificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notification = InAppNotification.Create(
            request.RecipientUserId,
            request.Title,
            request.Body,
            request.Category,
            request.RelatedEntityId,
            request.RelatedEntityType);

        dbContext.InAppNotifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var dto = new InAppNotificationDto(
            notification.Id,
            notification.Title,
            notification.Body,
            notification.Category,
            notification.RelatedEntityId,
            notification.RelatedEntityType,
            notification.CreatedAt);

        await pusher.PushToUserAsync(request.RecipientUserId, dto, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
