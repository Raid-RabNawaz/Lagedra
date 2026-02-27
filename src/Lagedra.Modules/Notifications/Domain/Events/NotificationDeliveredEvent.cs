using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Notifications.Domain.Events;

public sealed record NotificationDeliveredEvent(
    Guid NotificationId,
    DateTime DeliveredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
