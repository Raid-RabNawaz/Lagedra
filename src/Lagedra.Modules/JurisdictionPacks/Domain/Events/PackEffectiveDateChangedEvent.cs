using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.Events;

public sealed record PackEffectiveDateChangedEvent(
    Guid PackId,
    Guid VersionId,
    string FieldName,
    DateTime EffectiveDate) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
