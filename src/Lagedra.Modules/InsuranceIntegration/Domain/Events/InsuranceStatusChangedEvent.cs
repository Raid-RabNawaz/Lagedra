using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.InsuranceIntegration.Domain.Events;

public sealed record InsuranceStatusChangedEvent(
    Guid DealId,
    InsuranceState OldState,
    InsuranceState NewState) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
