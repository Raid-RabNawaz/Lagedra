using Lagedra.SharedKernel.Domain;

namespace Lagedra.Compliance.Domain;

/// <summary>
/// Append-only record of a rule violation associated with a deal.
/// Once created, violations are never deleted — they may be resolved or dismissed,
/// but the record persists permanently in the compliance ledger.
/// </summary>
public sealed class Violation : Entity<Guid>
{
    public Guid DealId { get; private set; }
    public Guid ReportedByUserId { get; private set; }
    public ViolationCategory Category { get; private set; }
    public ViolationStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? EvidenceReference { get; private set; }
    public DateTime DetectedAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    private Violation() { }

    public static Violation Record(
        Guid dealId,
        Guid reportedByUserId,
        ViolationCategory category,
        string description,
        string? evidenceReference = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        return new Violation
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            ReportedByUserId = reportedByUserId,
            Category = category,
            Status = ViolationStatus.Open,
            Description = description,
            EvidenceReference = evidenceReference,
            DetectedAt = DateTime.UtcNow
        };
    }

    public void MarkUnderReview()
    {
        if (Status != ViolationStatus.Open)
        {
            throw new InvalidOperationException($"Cannot review violation in status '{Status}'.");
        }

        Status = ViolationStatus.UnderReview;
    }

    public void Resolve()
    {
        if (Status is ViolationStatus.Resolved or ViolationStatus.Dismissed)
        {
            throw new InvalidOperationException($"Violation is already '{Status}'.");
        }

        Status = ViolationStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
    }

    public void Dismiss()
    {
        if (Status is ViolationStatus.Resolved or ViolationStatus.Dismissed)
        {
            throw new InvalidOperationException($"Violation is already '{Status}'.");
        }

        Status = ViolationStatus.Dismissed;
        ResolvedAt = DateTime.UtcNow;
    }

    public void Escalate()
    {
        if (Status is not (ViolationStatus.Open or ViolationStatus.UnderReview))
        {
            throw new InvalidOperationException($"Cannot escalate violation in status '{Status}'.");
        }

        Status = ViolationStatus.Escalated;
    }
}
