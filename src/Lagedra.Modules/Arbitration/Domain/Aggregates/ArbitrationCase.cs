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
    private readonly List<DecisionPenalty> _decisionPenalties = [];

    public Guid DealId { get; private set; }
    public Guid FiledByUserId { get; private set; }
    public ArbitrationTier Tier { get; private set; }
    public ArbitrationCategory Category { get; private set; }
    public ArbitrationStatus Status { get; private set; }
    public long FilingFeeCents { get; private set; }

    /// <summary>
    /// Stripe PaymentIntent collecting the filing fee from the filer. Null until
    /// a checkout is started. The fee is the platform's own adjudication revenue,
    /// so it settles into the platform balance (no host/Connect involvement).
    /// </summary>
    public string? FilingFeePaymentIntentId { get; private set; }

    /// <summary>When the filing fee was confirmed paid (case became active).</summary>
    public DateTime? FilingFeePaidAt { get; private set; }

    public DateTime FiledAt { get; private set; }
    public DateTime? EvidenceCompleteAt { get; private set; }
    public DateTime? DecisionDueAt { get; private set; }
    public string? DecisionSummary { get; private set; }
    public decimal? AwardAmount { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public bool IsStructuredVerdict { get; private set; }
    public DecisionOutcome? DecisionOutcome { get; private set; }
    public DecisionSeverity? DecisionSeverity { get; private set; }

    public IReadOnlyList<EvidenceSlot> EvidenceSlots => _evidenceSlots.AsReadOnly();
    public IReadOnlyList<ArbitratorAssignment> ArbitratorAssignments => _arbitratorAssignments.AsReadOnly();
    public IReadOnlyList<DecisionPenalty> DecisionPenalties => _decisionPenalties.AsReadOnly();

    private ArbitrationCase() { }

    public static ArbitrationCase File(
        Guid dealId, Guid filedByUserId, ArbitrationTier tier,
        ArbitrationCategory category, long filingFeeCents)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(filingFeeCents);

        var now = DateTime.UtcNow;
        var feeRequired = filingFeeCents > 0;
        var arbitrationCase = new ArbitrationCase
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            FiledByUserId = filedByUserId,
            Tier = tier,
            Category = category,
            FilingFeeCents = filingFeeCents,
            // Pay-to-activate: a case with a fee stays inert in PendingPayment
            // until the filer pays. A zero-fee case (admin set the fee to 0) is
            // active immediately.
            Status = feeRequired ? ArbitrationStatus.PendingPayment : ArbitrationStatus.Filed,
            FiledAt = now,
            CreatedAt = now
        };

        if (!feeRequired)
        {
            arbitrationCase.FilingFeePaidAt = now;
            arbitrationCase.AddDomainEvent(new CaseFiledEvent(arbitrationCase.Id, dealId, now));
        }

        return arbitrationCase;
    }

    /// <summary>
    /// Records the Stripe PaymentIntent that will collect the filing fee. Only
    /// valid while the case is awaiting payment.
    /// </summary>
    public void RecordFilingFeePaymentIntent(string paymentIntentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);

        if (Status != ArbitrationStatus.PendingPayment)
        {
            throw new InvalidOperationException(
                $"Cannot attach a filing-fee payment intent in status '{Status}'.");
        }

        FilingFeePaymentIntentId = paymentIntentId;
    }

    /// <summary>
    /// Marks the filing fee as paid and activates the case. Idempotent: a no-op
    /// if the case has already moved past <see cref="ArbitrationStatus.PendingPayment"/>
    /// (e.g. a duplicate webhook delivery).
    /// </summary>
    public void MarkFilingFeePaid()
    {
        if (Status != ArbitrationStatus.PendingPayment)
        {
            return;
        }

        var now = DateTime.UtcNow;
        Status = ArbitrationStatus.Filed;
        FilingFeePaidAt = now;

        AddDomainEvent(new CaseFiledEvent(Id, DealId, now));
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
        if (Status is ArbitrationStatus.PendingPayment or ArbitrationStatus.Decided or ArbitrationStatus.Appealed)
        {
            throw new InvalidOperationException($"Cannot assign arbitrator in status '{Status}'.");
        }

        var assignment = new ArbitratorAssignment(Id, arbitratorUserId, DateTime.UtcNow, concurrentCaseCount);
        _arbitratorAssignments.Add(assignment);
    }

    public void BeginReview()
    {
        if (_arbitratorAssignments.Count == 0)
        {
            throw new InvalidOperationException("An arbitrator must be assigned before beginning review.");
        }

        if (Status != ArbitrationStatus.EvidenceComplete)
        {
            throw new InvalidOperationException(
                $"Cannot begin review in status '{Status}'. Case must be evidence-complete.");
        }

        Status = ArbitrationStatus.UnderReview;
    }

    public void IssueDecision(
        string decisionSummary,
        decimal? awardAmount,
        bool isStructured,
        DecisionOutcome? outcome,
        DecisionSeverity? severity,
        IReadOnlyList<DecisionPenalty> penalties)
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

        if (isStructured && (outcome is null || severity is null))
        {
            throw new InvalidOperationException("Structured verdicts require outcome and severity.");
        }

        var now = DateTime.UtcNow;
        DecisionSummary = decisionSummary.Trim();
        AwardAmount = awardAmount;
        IsStructuredVerdict = isStructured;
        DecisionOutcome = isStructured ? outcome : null;
        DecisionSeverity = isStructured ? severity : null;
        DecidedAt = now;
        Status = ArbitrationStatus.Decided;

        _decisionPenalties.Clear();
        foreach (var penalty in penalties)
        {
            _decisionPenalties.Add(penalty);
        }

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
