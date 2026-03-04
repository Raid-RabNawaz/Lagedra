using Lagedra.SharedKernel.Domain;

namespace Lagedra.TruthSurface.Domain.Events;

public sealed record TruthSurfaceConfirmedEvent(
    Guid SnapshotId,
    Guid DealId,
    string Hash,
    string Signature,
    DateTime ConfirmedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
