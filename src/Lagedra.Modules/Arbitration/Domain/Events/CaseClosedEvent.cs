using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Arbitration.Domain.Events;

public sealed record CaseClosedEvent(
    Guid CaseId,
    Guid DealId,
    DateTime ClosedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
