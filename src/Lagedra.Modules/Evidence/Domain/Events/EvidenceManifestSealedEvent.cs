using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Evidence.Domain.Events;

public sealed record EvidenceManifestSealedEvent(
    Guid ManifestId,
    Guid DealId,
    string CompositeHash,
    DateTime SealedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
