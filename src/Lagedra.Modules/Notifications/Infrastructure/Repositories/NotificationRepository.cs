using Lagedra.Modules.Notifications.Domain.Aggregates;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Infrastructure.Repositories;

public sealed class NotificationRepository(NotificationDbContext dbContext)
{
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Notification>> GetFailedForRetryAsync(
        int maxAttempts,
        int batchSize,
        CancellationToken cancellationToken = default) =>
        await dbContext.Notifications
            .Where(n => n.Status == NotificationStatus.Failed && n.AttemptCount < maxAttempts)
            .OrderBy(n => n.UpdatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(Notification notification) =>
        dbContext.Notifications.Add(notification);
}
