using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised when a user confirms their phone number via SMS code. Subscribed by
/// Compliance to record a positive trust ledger entry.
/// </summary>
public sealed record PhoneVerifiedEvent(
    Guid UserId,
    DateTime VerifiedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
