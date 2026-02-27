using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record MarkAllNotificationsReadCommand(Guid UserId) : IRequest<Result<int>>;

public sealed class MarkAllNotificationsReadCommandHandler(NotificationDbContext dbContext)
    : IRequestHandler<MarkAllNotificationsReadCommand, Result<int>>
{
    public async Task<Result<int>> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var unread = await dbContext.InAppNotifications
            .Where(n => n.RecipientUserId == request.UserId && !n.IsRead)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var notification in unread)
        {
            notification.MarkRead();
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<int>.Success(unread.Count);
    }
}
