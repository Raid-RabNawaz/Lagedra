using Lagedra.Modules.VerificationAndRisk.Domain.Enums;

namespace Lagedra.Modules.VerificationAndRisk.Application.DTOs;

public sealed record RiskViewDto(
    Guid TenantUserId,
    VerificationClass VerificationClass,
    ConfidenceLevel ConfidenceLevel,
    string ConfidenceReason,
    long DepositBandLowCents,
    long DepositBandHighCents,
    DateTime ComputedAt);
