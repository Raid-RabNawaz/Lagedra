using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.RealTime;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Queries;

public sealed record GetAllNotificationsQuery(Guid UserId, int Limit = 100)
    : IRequest<Result<IReadOnlyList<InAppNotificationDto>>>;

public sealed class GetAllNotificationsQueryHandler(NotificationDbContext dbContext)
    : IRequestHandler<GetAllNotificationsQuery, Result<IReadOnlyList<InAppNotificationDto>>>
{
    public async Task<Result<IReadOnlyList<InAppNotificationDto>>> Handle(
        GetAllNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notifications = await dbContext.InAppNotifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == request.UserId)
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
