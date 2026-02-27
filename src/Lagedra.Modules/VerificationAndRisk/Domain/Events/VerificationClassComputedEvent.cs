using Lagedra.SharedKernel.Domain;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;

namespace Lagedra.Modules.VerificationAndRisk.Domain.Events;

public sealed record VerificationClassComputedEvent(
    Guid RiskProfileId,
    Guid TenantUserId,
    VerificationClass VerificationClass,
    ConfidenceLevel ConfidenceLevel,
    DateTime ComputedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
