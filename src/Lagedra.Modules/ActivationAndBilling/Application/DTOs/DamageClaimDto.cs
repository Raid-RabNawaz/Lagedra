using Lagedra.Modules.ActivationAndBilling.Domain.Enums;

namespace Lagedra.Modules.ActivationAndBilling.Application.DTOs;

public sealed record DamageClaimDto(
    Guid Id,
    Guid DealId,
    Guid ListingId,
    Guid FiledByUserId,
    Guid TenantUserId,
    DamageClaimStatus Status,
    string Description,
    long ClaimedAmountCents,
    long? ApprovedAmountCents,
    long DepositDeductionCents,
    long? InsuranceClaimCents,
    Guid? EvidenceManifestId,
    DateTime FiledAt,
    DateTime? ResolvedAt,
    string? ResolutionNotes);
