using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ComplianceMonitoring.Domain.Events;

public sealed record ViolationRecordedEvent(
    Guid ViolationId,
    Guid DealId,
    MonitoredViolationCategory Category,
    DateTime DetectedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
