using Lagedra.SharedKernel.Domain;

namespace Lagedra.TruthSurface.Domain.Events;

public sealed record TruthSurfaceSupersededEvent(
    Guid OriginalSnapshotId,
    Guid SupersedingSnapshotId,
    Guid DealId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
