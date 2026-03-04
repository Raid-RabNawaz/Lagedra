using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Notifications.Domain.Events;

public sealed record NotificationFailedEvent(
    Guid NotificationId,
    string Error) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
