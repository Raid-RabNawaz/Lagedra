using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record MarkNotificationReadCommand(Guid NotificationId, Guid UserId) : IRequest<Result>;

public sealed class MarkNotificationReadCommandHandler(NotificationDbContext dbContext)
    : IRequestHandler<MarkNotificationReadCommand, Result>
{
    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notification = await dbContext.InAppNotifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId
                && n.RecipientUserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (notification is null)
        {
            return Result.Failure(new Error("Notification.NotFound", "Notification not found."));
        }

        notification.MarkRead();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}

