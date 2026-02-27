using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

public sealed record ApplicationApprovedEvent(
    Guid ApplicationId,
    Guid DealId,
    Guid ListingId,
    Guid LandlordUserId,
    Guid TenantUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
