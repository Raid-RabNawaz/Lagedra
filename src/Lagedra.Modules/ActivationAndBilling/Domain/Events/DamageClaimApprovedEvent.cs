using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

public sealed record DamageClaimApprovedEvent(
    Guid ClaimId,
    Guid DealId,
    Guid TenantUserId,
    long ApprovedAmountCents) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
