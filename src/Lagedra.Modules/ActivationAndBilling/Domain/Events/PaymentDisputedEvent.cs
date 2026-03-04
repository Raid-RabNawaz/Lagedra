using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Events;

public sealed record PaymentDisputedEvent(
    Guid DealId,
    Guid TenantUserId,
    string Reason,
    Guid? EvidenceManifestId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
