using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Evidence.Domain.Events;

public sealed record EvidenceManifestCreatedEvent(
    Guid ManifestId,
    Guid DealId,
    string ManifestType) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
