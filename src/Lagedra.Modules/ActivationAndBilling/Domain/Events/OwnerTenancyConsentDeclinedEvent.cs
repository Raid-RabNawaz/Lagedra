using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

public sealed record OwnerTenancyConsentDeclinedEvent(
    Guid ApplicationId,
    Guid ListingId,
    Guid LandlordUserId,
    Guid TenantUserId,
    Guid HomeOwnerUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
