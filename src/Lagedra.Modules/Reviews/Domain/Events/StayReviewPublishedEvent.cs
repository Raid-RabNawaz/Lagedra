using Lagedra.Modules.Reviews.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Reviews.Domain.Events;

public sealed record StayReviewPublishedEvent(
    Guid ReviewId,
    Guid DealId,
    Guid ListingId,
    StayReviewDirection Direction,
    Guid ReviewerUserId,
    Guid RevieweeUserId,
    int OverallRating,
    DateTime PublishedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
