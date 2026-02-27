namespace Lagedra.Modules.VerificationAndRisk.Presentation.Contracts;

public sealed record RiskViewResponse(
    Guid TenantUserId,
    string VerificationClass,
    string ConfidenceLevel,
    string ConfidenceReason,
    long DepositBandLowCents,
    long DepositBandHighCents,
    DateTime ComputedAt);
