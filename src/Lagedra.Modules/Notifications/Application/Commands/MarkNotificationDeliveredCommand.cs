using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record MarkNotificationDeliveredCommand(
    Guid NotificationId,
    DateTime DeliveredAt) : IRequest<Result>;

public sealed class MarkNotificationDeliveredCommandHandler(
    NotificationDbContext dbContext)
    : IRequestHandler<MarkNotificationDeliveredCommand, Result>
{
    public async Task<Result> Handle(
        MarkNotificationDeliveredCommand request,
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

        notification.MarkDelivered(request.DeliveredAt);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
