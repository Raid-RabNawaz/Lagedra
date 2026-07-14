using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised when a stay is fully complete and eligible for host↔guest reviews:
/// deposit return settled, or move-out with zero deposit.
/// </summary>
public sealed record StayCompletedEvent(Guid DealId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
