using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

public sealed class DamageClaim : AggregateRoot<Guid>
{
    public Guid DealId { get; private set; }
    public Guid ListingId { get; private set; }
    public Guid FiledByUserId { get; private set; }
    public Guid TenantUserId { get; private set; }
    public DamageClaimStatus Status { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public long ClaimedAmountCents { get; private set; }
    public long? ApprovedAmountCents { get; private set; }
    public long DepositDeductionCents { get; private set; }
    public long? InsuranceClaimCents { get; private set; }
    public Guid? EvidenceManifestId { get; private set; }
    public DateTime FiledAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }

    private DamageClaim() { }

    public static DamageClaim File(
        Guid dealId,
        Guid listingId,
        Guid filedByUserId,
        Guid tenantUserId,
        string description,
        long claimedAmountCents,
        long depositAmountCents,
        Guid? evidenceManifestId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (claimedAmountCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(claimedAmountCents), "Claimed amount must be positive.");
        }

        var depositDeduction = Math.Min(claimedAmountCents, depositAmountCents);
        var insuranceClaim = claimedAmountCents > depositAmountCents
            ? claimedAmountCents - depositAmountCents
            : 0;

        var claim = new DamageClaim
        {
            Id = Guid.NewGuid(),
            DealId = dealId,
            ListingId = listingId,
            FiledByUserId = filedByUserId,
            TenantUserId = tenantUserId,
            Status = DamageClaimStatus.Filed,
            Description = description,
            ClaimedAmountCents = claimedAmountCents,
            DepositDeductionCents = depositDeduction,
            InsuranceClaimCents = insuranceClaim > 0 ? insuranceClaim : null,
            EvidenceManifestId = evidenceManifestId,
            FiledAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        claim.AddDomainEvent(new DamageClaimFiledEvent(
            claim.Id, dealId, listingId, filedByUserId, tenantUserId,
            claimedAmountCents, depositDeduction, insuranceClaim));

        return claim;
    }

    public void Approve(long approvedAmountCents, string? notes)
    {
        if (Status is not (DamageClaimStatus.Filed or DamageClaimStatus.UnderReview))
        {
            throw new InvalidOperationException($"Cannot approve claim in status '{Status}'.");
        }

        ApprovedAmountCents = approvedAmountCents;
        DepositDeductionCents = Math.Min(approvedAmountCents, DepositDeductionCents);
        InsuranceClaimCents = approvedAmountCents > DepositDeductionCents
            ? approvedAmountCents - DepositDeductionCents
            : null;
        Status = DamageClaimStatus.Approved;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = notes;

        AddDomainEvent(new DamageClaimApprovedEvent(Id, DealId, TenantUserId, approvedAmountCents));
    }

    public void PartiallyApprove(long approvedAmountCents, string? notes)
    {
        if (Status is not (DamageClaimStatus.Filed or DamageClaimStatus.UnderReview))
        {
            throw new InvalidOperationException($"Cannot approve claim in status '{Status}'.");
        }

        ApprovedAmountCents = approvedAmountCents;
        DepositDeductionCents = Math.Min(approvedAmountCents, DepositDeductionCents);
        InsuranceClaimCents = approvedAmountCents > DepositDeductionCents
            ? approvedAmountCents - DepositDeductionCents
            : null;
        Status = DamageClaimStatus.PartiallyApproved;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = notes;

        AddDomainEvent(new DamageClaimApprovedEvent(Id, DealId, TenantUserId, approvedAmountCents));
    }

    public void Reject(string? notes)
    {
        if (Status is not (DamageClaimStatus.Filed or DamageClaimStatus.UnderReview))
        {
            throw new InvalidOperationException($"Cannot reject claim in status '{Status}'.");
        }

        ApprovedAmountCents = 0;
        DepositDeductionCents = 0;
        InsuranceClaimCents = null;
        Status = DamageClaimStatus.Rejected;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = notes;

        AddDomainEvent(new DamageClaimRejectedEvent(Id, DealId, TenantUserId));
    }

    public void MarkUnderReview()
    {
        if (Status != DamageClaimStatus.Filed)
        {
            throw new InvalidOperationException($"Cannot mark as under review in status '{Status}'.");
        }

        Status = DamageClaimStatus.UnderReview;
    }
}
