using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Arbitration.Domain.Events;

public sealed record EvidenceCompleteEvent(
    Guid CaseId,
    DateTime EvidenceCompleteAt,
    DateTime DecisionDueAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
