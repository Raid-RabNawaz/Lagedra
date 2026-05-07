using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

public sealed record DealCompletedEvent(
    Guid BillingAccountId,
    Guid DealId,
    Guid LandlordUserId,
    Guid TenantUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
