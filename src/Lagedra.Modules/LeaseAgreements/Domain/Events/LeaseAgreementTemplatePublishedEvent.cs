using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.LeaseAgreements.Domain.Events;

public sealed record LeaseAgreementTemplatePublishedEvent(
    Guid TemplateId,
    string JurisdictionCode,
    Guid VersionId,
    int VersionNumber) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
