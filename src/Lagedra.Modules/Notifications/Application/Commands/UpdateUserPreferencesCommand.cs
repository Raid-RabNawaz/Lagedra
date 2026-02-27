using Lagedra.Modules.Notifications.Application.DTOs;
using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record UpdateUserPreferencesCommand(
    Guid UserId,
    Dictionary<string, bool> EventOptIns) : IRequest<Result<NotificationPreferencesDto>>;

public sealed class UpdateUserPreferencesCommandHandler(
    NotificationDbContext dbContext)
    : IRequestHandler<UpdateUserPreferencesCommand, Result<NotificationPreferencesDto>>
{
    public async Task<Result<NotificationPreferencesDto>> Handle(
        UpdateUserPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prefs = await dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (prefs is null)
        {
            prefs = new UserNotificationPreferences(request.UserId);
            dbContext.UserPreferences.Add(prefs);
        }

        foreach (var kvp in request.EventOptIns)
        {
            prefs.SetEventOptIn(kvp.Key, kvp.Value);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<NotificationPreferencesDto>.Success(
            new NotificationPreferencesDto(
                prefs.UserId,
                prefs.EventOptIns,
                prefs.TransactionalAlwaysSent));
    }
}
