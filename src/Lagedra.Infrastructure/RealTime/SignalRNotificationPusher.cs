using Lagedra.SharedKernel.RealTime;
using Microsoft.AspNetCore.SignalR;

namespace Lagedra.Infrastructure.RealTime;

public sealed class SignalRNotificationPusher(IHubContext<NotificationHub> hubContext) : INotificationPusher
{
    public async Task PushToUserAsync(Guid userId, InAppNotificationDto notification, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(notification);

        await hubContext.Clients
            .Group($"user:{userId}")
            .SendAsync("ReceiveNotification", notification, ct)
            .ConfigureAwait(false);
    }

    public async Task PushToUsersAsync(
        IEnumerable<Guid> userIds,
        InAppNotificationDto notification,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentNullException.ThrowIfNull(notification);

        var groups = userIds.Select(id => $"user:{id}").ToList();
        await hubContext.Clients
            .Groups(groups)
            .SendAsync("ReceiveNotification", notification, ct)
            .ConfigureAwait(false);
    }
}
