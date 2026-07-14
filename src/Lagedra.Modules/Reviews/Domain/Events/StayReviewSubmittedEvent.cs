using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Reviews.Domain.Events;

public sealed record StayReviewSubmittedEvent(
    Guid ReviewId,
    Guid DealId,
    StayReviewDirection Direction,
    Guid ReviewerUserId,
    Guid RevieweeUserId,
    DateTime SubmittedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
