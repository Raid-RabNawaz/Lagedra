using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Arbitration.Domain.Events;

public sealed record CaseAppealedEvent(
    Guid CaseId,
    Guid DealId,
    Guid AppealedByUserId,
    string Reason,
    DateTime AppealedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
