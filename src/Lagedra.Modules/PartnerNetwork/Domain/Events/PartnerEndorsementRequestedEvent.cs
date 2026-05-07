using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.PartnerNetwork.Domain.Events;

/// <summary>
/// Raised when an endorsement request is created. Local to the PartnerNetwork module
/// (no cross-module subscribers); promoted to a SharedKernel integration event only if
/// another module needs to react.
/// </summary>
public sealed record PartnerEndorsementRequestedEvent(
    Guid EndorsementId,
    Guid OrganizationId,
    Guid TenantUserId,
    Guid RequestedByUserId,
    DateTime RequestedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
