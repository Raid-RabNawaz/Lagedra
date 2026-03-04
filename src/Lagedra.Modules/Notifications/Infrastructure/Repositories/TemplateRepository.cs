using Lagedra.Modules.Notifications.Domain.Entities;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Notifications.Infrastructure.Repositories;

public sealed class TemplateRepository(NotificationDbContext dbContext)
{
    public async Task<NotificationTemplate?> GetByTemplateIdAsync(
        string templateId,
        NotificationChannel channel,
        CancellationToken cancellationToken = default) =>
        await dbContext.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateId == templateId && t.Channel == channel, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<NotificationTemplate>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Templates
            .AsNoTracking()
            .OrderBy(t => t.TemplateId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
