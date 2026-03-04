using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.PartnerNetwork.Domain.Events;

public sealed record PartnerOrganizationSuspendedEvent(
    Guid OrganizationId,
    string Reason) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
