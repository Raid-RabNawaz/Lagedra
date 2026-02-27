using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.PartnerNetwork.Domain.Events;

public sealed record PartnerOrganizationVerifiedEvent(
    Guid OrganizationId,
    string Name,
    Guid VerifiedBy) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
