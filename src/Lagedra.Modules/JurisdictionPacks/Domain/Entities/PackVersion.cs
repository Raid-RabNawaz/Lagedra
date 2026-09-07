using Lagedra.Modules.JurisdictionPacks.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.JurisdictionPacks.Domain.Entities;

public sealed class PackVersion : Entity<Guid>
{
    public Guid PackId { get; private set; }
    public int VersionNumber { get; private set; }
    public PackVersionStatus Status { get; private set; }
    public DateTime? EffectiveDate { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public Guid? SecondApproverId { get; private set; }

    private readonly List<EffectiveDateRule> _effectiveDateRules = [];
    private readonly List<FieldGatingRule> _fieldGatingRules = [];
    private readonly List<EvidenceSchedule> _evidenceSchedules = [];
    private readonly List<DepositCapRule> _depositCapRules = [];

    public IReadOnlyList<EffectiveDateRule> EffectiveDateRules => _effectiveDateRules.AsReadOnly();
    public IReadOnlyList<FieldGatingRule> FieldGatingRules => _fieldGatingRules.AsReadOnly();
    public IReadOnlyList<EvidenceSchedule> EvidenceSchedules => _evidenceSchedules.AsReadOnly();
    public IReadOnlyList<DepositCapRule> DepositCapRules => _depositCapRules.AsReadOnly();

    private PackVersion() { }

    internal static PackVersion Create(Guid packId, int versionNumber)
    {
        return new PackVersion
        {
            Id = Guid.NewGuid(),
            PackId = packId,
            VersionNumber = versionNumber,
            Status = PackVersionStatus.Draft
        };
    }

    public void SetEffectiveDate(DateTime effectiveDate)
    {
        EnsureDraft();
        EffectiveDate = effectiveDate;
    }

    // These return the created rule so callers persisting an already-tracked
    // version can register it for insert. The rule Ids are assigned here, and a
    // persistence layer that infers state from the key would otherwise treat
    // each new rule as an existing row.
    public EffectiveDateRule AddEffectiveDateRule(string fieldName, DateTime effectiveDate)
    {
        EnsureDraft();
        var rule = EffectiveDateRule.Create(Id, fieldName, effectiveDate);
        _effectiveDateRules.Add(rule);
        return rule;
    }

    public FieldGatingRule AddFieldGatingRule(string fieldName, GatingType gatingType, string value, string? condition)
    {
        EnsureDraft();
        var rule = FieldGatingRule.Create(Id, fieldName, gatingType, value, condition);
        _fieldGatingRules.Add(rule);
        return rule;
    }

    public EvidenceSchedule AddEvidenceSchedule(string category, string minimumRequirements)
    {
        EnsureDraft();
        var schedule = EvidenceSchedule.Create(Id, category, minimumRequirements);
        _evidenceSchedules.Add(schedule);
        return schedule;
    }

    public DepositCapRule AddDepositCapRule(
        string jurisdictionCode,
        decimal maxMultiplier,
        string legalReference,
        string? exceptionCondition = null,
        decimal? exceptionMultiplier = null)
    {
        EnsureDraft();
        var rule = DepositCapRule.Create(
            Id, jurisdictionCode, maxMultiplier, legalReference,
            exceptionCondition, exceptionMultiplier);
        _depositCapRules.Add(rule);
        return rule;
    }

    public void RequestApproval()
    {
        EnsureDraft();

        if (EffectiveDate is null)
        {
            throw new InvalidOperationException("Effective date must be set before requesting approval.");
        }

        Status = PackVersionStatus.PendingApproval;
    }

    public void Approve(Guid userId)
    {
        if (Status != PackVersionStatus.PendingApproval)
        {
            throw new InvalidOperationException($"Cannot approve a version in status '{Status}'.");
        }

        if (ApprovedBy is null)
        {
            ApprovedBy = userId;
            ApprovedAt = DateTime.UtcNow;
            return;
        }

        if (ApprovedBy == userId)
        {
            throw new InvalidOperationException("The same user cannot provide both approvals (dual-control).");
        }

        SecondApproverId = userId;
        Status = PackVersionStatus.Active;
    }

    public bool HasDualApproval => ApprovedBy.HasValue && SecondApproverId.HasValue;

    public void Deprecate()
    {
        if (Status != PackVersionStatus.Active)
        {
            throw new InvalidOperationException($"Only active versions can be deprecated. Current status: '{Status}'.");
        }

        Status = PackVersionStatus.Deprecated;
    }

    private void EnsureDraft()
    {
        if (Status != PackVersionStatus.Draft)
        {
            throw new InvalidOperationException($"Version must be in Draft status. Current status: '{Status}'.");
        }
    }
}
