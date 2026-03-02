using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

public sealed record DamageClaimRejectedEvent(
    Guid ClaimId,
    Guid DealId,
    Guid TenantUserId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
