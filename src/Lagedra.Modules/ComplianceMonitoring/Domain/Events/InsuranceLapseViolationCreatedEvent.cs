using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ComplianceMonitoring.Domain.Events;

public sealed record InsuranceLapseViolationCreatedEvent(
    Guid ViolationId,
    Guid DealId,
    DateTime DetectedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
