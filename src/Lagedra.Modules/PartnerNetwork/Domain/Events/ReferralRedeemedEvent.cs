using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.PartnerNetwork.Domain.Events;

public sealed record ReferralRedeemedEvent(
    Guid OrganizationId,
    Guid ReferralLinkId,
    Guid RedeemedByUserId,
    string OrganizationName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
