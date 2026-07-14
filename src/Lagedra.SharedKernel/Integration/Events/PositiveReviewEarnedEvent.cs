using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised when a published stay review has overall rating &gt;= 4 so Compliance
/// can record a public PositiveReview trust-ledger entry for the reviewee.
/// </summary>
public sealed record PositiveReviewEarnedEvent(
    Guid DealId,
    Guid RevieweeUserId,
    int OverallRating) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
