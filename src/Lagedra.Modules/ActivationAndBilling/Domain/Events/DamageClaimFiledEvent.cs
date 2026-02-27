using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

public sealed record DamageClaimFiledEvent(
    Guid ClaimId,
    Guid DealId,
    Guid ListingId,
    Guid FiledByUserId,
    Guid TenantUserId,
    long ClaimedAmountCents,
    long DepositDeductionCents,
    long InsuranceClaimCents) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
