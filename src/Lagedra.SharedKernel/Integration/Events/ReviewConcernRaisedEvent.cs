using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised when a published stay review has overall rating &lt;= 2 so Compliance
/// can record a public ReviewConcern trust-ledger entry for the reviewee.
/// This is a soft reputation signal — it does not create a compliance Violation.
/// </summary>
public sealed record ReviewConcernRaisedEvent(
    Guid DealId,
    Guid RevieweeUserId,
    int OverallRating) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
