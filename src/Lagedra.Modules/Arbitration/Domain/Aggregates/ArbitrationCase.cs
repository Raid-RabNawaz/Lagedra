using Lagedra.Modules.Arbitration.Domain.Entities;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Domain.Events;
using Lagedra.Modules.Arbitration.Domain.Policies;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Arbitration.Domain.Aggregates;

public sealed class ArbitrationCase : AggregateRoot<Guid>
{
    private readonly List<EvidenceSlot> _evidenceSlots = [];
    private readonly List<ArbitratorAssignment> _arbitratorAssignments = [];

    public Guid DealId { get; private set; }
    public Guid FiledByUserId { get; private set; }
    public ArbitrationTier Tier { get; private set; }
    public ArbitrationCategory Category { get; private set; }
    public ArbitrationStatus Status { get; private set; }
    public long FilingFeeCents { get; private set; }
    public DateTime FiledAt { get; private set; }
    public DateTime? EvidenceCompleteAt { get; private set; }
    public DateTime? DecisionDueAt { get; private set; }
    public string? DecisionSummary { get; private set; }
    public decimal? AwardAmount { get; private set; }
    public DateTime? DecidedAt { get; private set; }

    public IReadOnlyList<EvidenceSlot> EvidenceSlots => _evidenceSlots.AsReadOnly();
    public IReadOnlyList<ArbitratorAssignment> ArbitratorAssignments => _arbitratorAssignments.AsReadOnly();

    private ArbitrationCase() { }

    public static ArbitrationCase File(
        Guid dealId, Guid filedByUserId, ArbitrationTier tier,
        ArbitrationCategory category, long filingFeeCents)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(filingFeeCents);

        var now = DateTime.UtcNow;
        var arbitrationCase = new ArbitrationCase
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            FiledByUserId = filedByUserId,
            Tier = tier,
            Category = category,
            FilingFeeCents = filingFeeCents,
            Status = ArbitrationStatus.Filed,
            FiledAt = now,
            CreatedAt = now
        };

        arbitrationCase.AddDomainEvent(new CaseFiledEvent(arbitrationCase.Id, dealId, now));
        return arbitrationCase;
    }

    public void AttachEvidence(string slotType, Guid submittedBy, Guid evidenceManifestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slotType);

        if (Status is not (ArbitrationStatus.Filed or ArbitrationStatus.EvidencePending
                        or ArbitrationStatus.Appealed))
        {
            throw new InvalidOperationException($"Cannot attach evidence in status '{Status}'.");
        }

        var slot = new EvidenceSlot(Id, slotType, submittedBy, evidenceManifestId, DateTime.UtcNow);
        _evidenceSlots.Add(slot);

        if (Status is ArbitrationStatus.Filed or ArbitrationStatus.Appealed)
        {
            Status = ArbitrationStatus.EvidencePending;
        }
    }

    public void MarkEvidenceComplete()
    {
        if (Status is not (ArbitrationStatus.Filed or ArbitrationStatus.EvidencePending))
        {
            throw new InvalidOperationException($"Cannot mark evidence complete in status '{Status}'.");
        }

        if (!EvidenceMinimumThresholdPolicy.IsSatisfied(Category, _evidenceSlots.Count))
        {
            throw new InvalidOperationException(
                $"Minimum evidence threshold not met for category '{Category}'. " +
                $"Required: {EvidenceMinimumThresholdPolicy.GetMinimumSlots(Category)}, submitted: {_evidenceSlots.Count}.");
        }

        var now = DateTime.UtcNow;
        Status = ArbitrationStatus.EvidenceComplete;
        EvidenceCompleteAt = now;
        DecisionDueAt = now.AddDays(14);

        AddDomainEvent(new EvidenceCompleteEvent(Id, now, DecisionDueAt.Value));
    }

    public void AssignArbitrator(Guid arbitratorUserId, int concurrentCaseCount)
    {
        if (Status is ArbitrationStatus.Decided or ArbitrationStatus.Appealed)
        {
            throw new InvalidOperationException($"Cannot assign arbitrator in status '{Status}'.");
        }

        var assignment = new ArbitratorAssignment(Id, arbitratorUserId, DateTime.UtcNow, concurrentCaseCount);
        _arbitratorAssignments.Add(assignment);
        Status = ArbitrationStatus.UnderReview;
    }

    public void IssueDecision(string decisionSummary, decimal? awardAmount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionSummary);

        if (Status is not (ArbitrationStatus.EvidenceComplete or ArbitrationStatus.UnderReview))
        {
            throw new InvalidOperationException($"Cannot issue decision in status '{Status}'.");
        }

        if (_arbitratorAssignments.Count == 0)
        {
            throw new InvalidOperationException("An arbitrator must be assigned before issuing a decision.");
        }

        var now = DateTime.UtcNow;
        DecisionSummary = decisionSummary;
        AwardAmount = awardAmount;
        DecidedAt = now;
        Status = ArbitrationStatus.Decided;

        AddDomainEvent(new DecisionIssuedEvent(Id, DealId, Tier, now));
    }

    public void CloseCase()
    {
        if (Status != ArbitrationStatus.Decided)
        {
            throw new InvalidOperationException($"Cannot close case in status '{Status}'. Only decided cases can be closed.");
        }

        var now = DateTime.UtcNow;
        Status = ArbitrationStatus.Closed;

        AddDomainEvent(new CaseClosedEvent(Id, DealId, now));
    }

    public void Appeal(Guid appealedByUserId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != ArbitrationStatus.Decided)
        {
            throw new InvalidOperationException($"Cannot appeal case in status '{Status}'. Only decided cases can be appealed.");
        }

        var now = DateTime.UtcNow;
        Status = ArbitrationStatus.Appealed;
        EvidenceCompleteAt = null;
        DecisionDueAt = null;

        AddDomainEvent(new CaseAppealedEvent(Id, DealId, appealedByUserId, reason, now));
    }

    internal void RaiseBacklogEscalation(int overdueCaseCount) =>
        AddDomainEvent(new ArbitrationBacklogEscalationEvent(overdueCaseCount, DateTime.UtcNow));
}
