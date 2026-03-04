using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Arbitration.Domain.Events;

public sealed record DecisionIssuedEvent(
    Guid CaseId,
    Guid DealId,
    ArbitrationTier Tier,
    DateTime DecidedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
