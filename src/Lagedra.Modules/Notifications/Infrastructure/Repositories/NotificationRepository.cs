using Lagedra.Modules.Notifications.Domain.Aggregates;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Infrastructure.Repositories;

public sealed class NotificationRepository(NotificationDbContext dbContext)
{
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public void Add(Notification notification) =>
        dbContext.Notifications.Add(notification);
}
