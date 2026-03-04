using Lagedra.Modules.ComplianceMonitoring.Domain.Enums;
using Lagedra.Modules.ComplianceMonitoring.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ComplianceMonitoring.Domain.Entities;

public sealed class MonitoredViolation : Entity<Guid>
{
    public Guid DealId { get; private set; }
    public MonitoredViolationCategory Category { get; private set; }
    public DateTime DetectedAt { get; private set; }
    public DateTime? CureDeadline { get; private set; }
    public MonitoredViolationStatus Status { get; private set; }

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    private MonitoredViolation() { }

    public static MonitoredViolation Create(
        Guid dealId,
        MonitoredViolationCategory category,
        DateTime detectedAt,
        DateTime? cureDeadline)
    {
        var violation = new MonitoredViolation
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            Category = category,
            DetectedAt = detectedAt,
            CureDeadline = cureDeadline,
            Status = MonitoredViolationStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        violation._domainEvents.Add(new ViolationRecordedEvent(
            violation.Id, dealId, category, detectedAt));

        return violation;
    }

    public void Cure()
    {
        if (Status != MonitoredViolationStatus.Open)
        {
            throw new InvalidOperationException($"Cannot cure violation in status '{Status}'.");
        }

        Status = MonitoredViolationStatus.Cured;
    }

    public void Escalate()
    {
        if (Status != MonitoredViolationStatus.Open)
        {
            throw new InvalidOperationException($"Cannot escalate violation in status '{Status}'.");
        }

        Status = MonitoredViolationStatus.Escalated;
    }
}
