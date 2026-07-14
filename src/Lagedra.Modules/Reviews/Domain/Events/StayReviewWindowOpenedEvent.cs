using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Reviews.Domain.Events;

public sealed record StayReviewWindowOpenedEvent(
    Guid WindowId,
    Guid DealId,
    Guid LandlordUserId,
    Guid TenantUserId,
    DateTime ClosesAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
