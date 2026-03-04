using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.VerificationAndRisk.Domain.Events;

public sealed record DepositBandUpdatedEvent(
    Guid RiskProfileId,
    Guid TenantUserId,
    long DepositBandLowCents,
    long DepositBandHighCents) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
