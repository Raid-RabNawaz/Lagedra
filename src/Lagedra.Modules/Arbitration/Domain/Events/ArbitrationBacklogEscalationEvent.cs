using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Arbitration.Domain.Events;

public sealed record ArbitrationBacklogEscalationEvent(
    int OverdueCaseCount,
    DateTime CheckedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
