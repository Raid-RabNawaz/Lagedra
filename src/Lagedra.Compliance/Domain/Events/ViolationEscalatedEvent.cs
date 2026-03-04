using Lagedra.SharedKernel.Domain;

namespace Lagedra.Compliance.Domain.Events;

public sealed record ViolationEscalatedEvent(
    Guid ViolationId,
    Guid DealId,
    Guid TargetUserId,
    ViolationCategory Category) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
