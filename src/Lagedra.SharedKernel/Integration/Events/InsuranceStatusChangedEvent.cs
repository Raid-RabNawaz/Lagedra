using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

public sealed record InsuranceStatusChangedEvent(
    Guid DealId,
    InsuranceState OldState,
    InsuranceState NewState) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
