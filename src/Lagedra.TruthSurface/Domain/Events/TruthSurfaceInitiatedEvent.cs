using Lagedra.SharedKernel.Domain;

namespace Lagedra.TruthSurface.Domain.Events;

public sealed record TruthSurfaceInitiatedEvent(
    Guid SnapshotId,
    Guid DealId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
