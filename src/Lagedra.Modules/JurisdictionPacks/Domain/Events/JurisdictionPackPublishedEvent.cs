using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.Events;

public sealed record JurisdictionPackPublishedEvent(
    Guid PackId,
    string JurisdictionCode,
    Guid VersionId,
    int VersionNumber) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
