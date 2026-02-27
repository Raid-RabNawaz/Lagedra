using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Queries;

public sealed record GetUnreadCountQuery(Guid UserId) : IRequest<Result<int>>;

public sealed class GetUnreadCountQueryHandler(NotificationDbContext dbContext)
    : IRequestHandler<GetUnreadCountQuery, Result<int>>
{
    public async Task<Result<int>> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var count = await dbContext.InAppNotifications
            .CountAsync(n => n.RecipientUserId == request.UserId && !n.IsRead, cancellationToken)
            .ConfigureAwait(false);

        return Result<int>.Success(count);
    }
}
