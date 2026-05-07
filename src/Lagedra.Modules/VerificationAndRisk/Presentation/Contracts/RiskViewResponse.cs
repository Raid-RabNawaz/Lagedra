namespace Lagedra.Modules.VerificationAndRisk.Presentation.Contracts;

public sealed record RiskViewResponse(
    Guid TenantUserId,
    string VerificationClass,
    string ConfidenceLevel,
    string ConfidenceReason,
    long DepositBandLowCents,
    long DepositBandHighCents,
    DateTime ComputedAt,
    string ProtectionTier,
    IReadOnlyList<EndorsementSummaryResponse> EndorsedBy);

public sealed record EndorsementSummaryResponse(
    Guid EndorsementId,
    Guid OrganizationId,
    string OrganizationName,
    DateTime ApprovedAt,
    DateTime ExpiresAt);
