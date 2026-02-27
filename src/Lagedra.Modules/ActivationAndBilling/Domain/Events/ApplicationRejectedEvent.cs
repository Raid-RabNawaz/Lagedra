using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

public sealed record ApplicationRejectedEvent(
    Guid ApplicationId,
    Guid ListingId,
    Guid LandlordUserId,
    Guid TenantUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
