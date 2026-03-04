using Lagedra.SharedKernel.Domain;

namespace Lagedra.Compliance.Domain;

/// <summary>
/// Lightweight inbound signal — for example from a webhook, a scheduled job,
/// or another module — indicating something compliance-relevant happened.
/// The compliance engine processes these to decide whether a violation
/// should be opened or a ledger entry recorded.
/// </summary>
public sealed class ComplianceSignal : Entity<Guid>
{
    public Guid DealId { get; private set; }
    public string SignalType { get; private set; } = string.Empty;
    public string? Payload { get; private set; }
    public DateTime ReceivedAt { get; private set; }
    public bool Processed { get; private set; }

    private ComplianceSignal() { }

    public static ComplianceSignal Create(Guid dealId, string signalType, string? payload = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signalType);

        return new ComplianceSignal
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            SignalType = signalType,
            Payload = payload,
            ReceivedAt = DateTime.UtcNow
        };
    }

    public void MarkProcessed()
    {
        Processed = true;
    }
}
