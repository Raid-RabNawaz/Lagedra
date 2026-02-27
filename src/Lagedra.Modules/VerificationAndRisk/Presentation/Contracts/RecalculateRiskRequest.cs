using Lagedra.Modules.VerificationAndRisk.Domain.Enums;

namespace Lagedra.Modules.VerificationAndRisk.Presentation.Contracts;

public sealed record RecalculateRiskRequest(
    IdentityVerificationStatus IdentityStatus,
    BackgroundCheckStatus BackgroundStatus,
    InsuranceStatus InsuranceStatus,
    int ViolationCount);
