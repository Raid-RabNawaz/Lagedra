using Lagedra.Modules.Notifications.Application.DTOs;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Queries;

public sealed record ListNotificationHistoryQuery(Guid UserId) : IRequest<Result<IReadOnlyList<NotificationDto>>>;

public sealed class ListNotificationHistoryQueryHandler(
    NotificationDbContext dbContext)
    : IRequestHandler<ListNotificationHistoryQuery, Result<IReadOnlyList<NotificationDto>>>
{
    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(
        ListNotificationHistoryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var notifications = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == request.UserId)
            .OrderByDescending(n => n.ScheduledAt)
            .Select(n => new NotificationDto(
                n.Id,
                n.RecipientUserId,
                n.RecipientEmail,
                n.Channel,
                n.TemplateId,
                n.Status,
                n.ScheduledAt,
                n.SentAt,
                n.AttemptCount))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<NotificationDto>>.Success(notifications);
    }
}
