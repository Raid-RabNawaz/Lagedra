using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ComplianceMonitoring.Domain.Entities;

public sealed class MonitoredComplianceSignal : Entity<Guid>
{
    public Guid DealId { get; private set; }
    public string SignalType { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public DateTime ReceivedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private MonitoredComplianceSignal() { }

    public static MonitoredComplianceSignal Record(
        Guid dealId,
        string signalType,
        string source,
        DateTime receivedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signalType);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return new MonitoredComplianceSignal
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            SignalType = signalType,
            Source = source,
            ReceivedAt = receivedAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed(DateTime processedAt)
    {
        if (ProcessedAt is not null)
        {
            throw new InvalidOperationException("Signal has already been processed.");
        }

        ProcessedAt = processedAt;
    }
}
