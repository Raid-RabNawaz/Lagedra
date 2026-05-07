using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

public sealed record BillingStoppedEvent(
    Guid BillingAccountId,
    Guid DealId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
