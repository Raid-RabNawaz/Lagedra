using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Privacy.Domain.Events;

public sealed record LegalHoldAppliedEvent(
    Guid LegalHoldId,
    Guid UserId,
    string Reason,
    DateTime AppliedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
