using Lagedra.Modules.Notifications.Application.DTOs;
using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Queries;

public sealed record GetUserPreferencesQuery(Guid UserId) : IRequest<Result<NotificationPreferencesDto>>;

public sealed class GetUserPreferencesQueryHandler(
    NotificationDbContext dbContext)
    : IRequestHandler<GetUserPreferencesQuery, Result<NotificationPreferencesDto>>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(
        GetUserPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prefs = await dbContext.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (prefs is null)
        {
            prefs = new UserNotificationPreferences(request.UserId);
        }

        return Result<NotificationPreferencesDto>.Success(
            new NotificationPreferencesDto(
                prefs.UserId,
                prefs.EventOptIns,
                prefs.TransactionalAlwaysSent));
    }
}
