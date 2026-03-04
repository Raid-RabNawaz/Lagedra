using Lagedra.SharedKernel.Domain;

namespace Lagedra.Compliance.Domain.Events;

public sealed record ViolationResolvedEvent(
    Guid ViolationId,
    Guid DealId,
    Guid TargetUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
