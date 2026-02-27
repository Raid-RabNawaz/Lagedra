using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Notifications.Domain.Events;

public sealed record NotificationQueuedEvent(
    Guid NotificationId,
    Guid RecipientUserId,
    string TemplateId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
