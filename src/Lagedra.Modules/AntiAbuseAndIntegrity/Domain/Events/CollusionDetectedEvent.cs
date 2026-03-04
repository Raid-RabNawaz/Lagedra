using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Events;

public sealed record CollusionDetectedEvent(
    Guid AbuseCaseId,
    Guid PartyAUserId,
    Guid PartyBUserId,
    int RepeatedDealCount) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
