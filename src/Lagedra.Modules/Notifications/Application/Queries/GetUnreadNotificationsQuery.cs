using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.RealTime;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Queries;

public sealed record GetUnreadNotificationsQuery(Guid UserId, int Limit = 50)
    : IRequest<Result<IReadOnlyList<InAppNotificationDto>>>;

public sealed class GetUnreadNotificationsQueryHandler(NotificationDbContext dbContext)
    : IRequestHandler<GetUnreadNotificationsQuery, Result<IReadOnlyList<InAppNotificationDto>>>
{
    public async Task<Result<IReadOnlyList<InAppNotificationDto>>> Handle(
        GetUnreadNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notifications = await dbContext.InAppNotifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == request.UserId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(request.Limit)
            .Select(n => new InAppNotificationDto(
                n.Id, n.Title, n.Body, n.Category,
                n.RelatedEntityId, n.RelatedEntityType, n.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<InAppNotificationDto>>.Success(notifications);
    }
}
