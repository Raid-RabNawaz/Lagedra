using Lagedra.Modules.VerificationAndRisk.Domain.Enums;

namespace Lagedra.Modules.VerificationAndRisk.Domain.ValueObjects;

public sealed record VerificationInput(
    IdentityVerificationStatus IdentityStatus,
    BackgroundCheckStatus BackgroundStatus,
    InsuranceStatus InsuranceStatus,
    int ViolationCount);
