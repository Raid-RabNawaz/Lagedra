using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised when a host reports that a monthly rent payment (months 2+, paid
/// directly to the host outside the platform) was not received. Feeds the
/// compliance module as a PaymentDefault signal.
/// </summary>
public sealed record RentMissedEvent(
    Guid DealId,
    Guid LandlordUserId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
