using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

/// <summary>
/// Raised when a still-pending reservation request lapses because the host did
/// not decide within the booking-request window (default 72h). Lets the tenant
/// be told their request expired so they can re-request or look elsewhere.
/// </summary>
public sealed record ApplicationExpiredEvent(
    Guid ApplicationId,
    Guid ListingId,
    Guid LandlordUserId,
    Guid TenantUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
