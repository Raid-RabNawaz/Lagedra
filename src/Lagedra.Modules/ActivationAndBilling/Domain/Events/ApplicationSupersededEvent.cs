using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

/// <summary>
/// Raised when a pending reservation request is automatically rejected because
/// the host accepted a different, date-overlapping request for the same
/// listing. The request ends in <c>Rejected</c> status just like a manual
/// decline, but a distinct event lets us tell the tenant *why* (someone else's
/// overlapping booking was confirmed) instead of implying the host declined them.
/// </summary>
public sealed record ApplicationSupersededEvent(
    Guid ApplicationId,
    Guid ListingId,
    Guid LandlordUserId,
    Guid TenantUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
