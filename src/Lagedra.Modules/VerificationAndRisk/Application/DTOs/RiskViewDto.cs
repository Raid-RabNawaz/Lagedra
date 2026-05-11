using Lagedra.Modules.VerificationAndRisk.Domain.Enums;

namespace Lagedra.Modules.VerificationAndRisk.Application.DTOs;

public sealed record RiskViewDto(
    Guid TenantUserId,
    VerificationClass VerificationClass,
    ConfidenceLevel ConfidenceLevel,
    string ConfidenceReason,
    long DepositBandLowCents,
    long DepositBandHighCents,
    DateTime ComputedAt,
    ProtectionTier ProtectionTier,
    IReadOnlyList<EndorsementSummaryDto> EndorsedBy);

public sealed record EndorsementSummaryDto(
    Guid EndorsementId,
    Guid OrganizationId,
    string OrganizationName,
    DateTime ApprovedAt,
    DateTime ExpiresAt);

/// <summary>
/// Computed protection tier surfaced to the frontend so it never has to derive
/// the user-facing label from <c>InsuranceStatus</c> + <c>EndorsedBy</c> directly.
///
/// <para>Note: when a tenant has BOTH an active partner endorsement AND a real
/// third-party insurance binding, the tier reports <see cref="PartnerBacked"/>
/// (the more informative attribution) — the deposit band is computed from
/// <see cref="InsuranceStatus.InstitutionBacked"/> in either case, so there is no
/// double-discount.</para>
/// </summary>
public enum ProtectionTier
{
    Uninsured,
    ThirdPartyInsured,
    PartnerBacked
}
