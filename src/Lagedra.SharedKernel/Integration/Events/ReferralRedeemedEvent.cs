using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

public sealed record ReferralRedeemedEvent(
    Guid OrganizationId,
    Guid ReferralLinkId,
    Guid RedeemedByUserId,
    string OrganizationName) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
