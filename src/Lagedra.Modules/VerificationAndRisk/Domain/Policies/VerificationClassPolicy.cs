using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.Modules.VerificationAndRisk.Domain.ValueObjects;

namespace Lagedra.Modules.VerificationAndRisk.Domain.Policies;

/// <summary>
/// Deterministic verification class assignment. No ML, no actuarial model.
/// Protected-class attributes (race, gender, religion, etc.) are never used as inputs.
/// </summary>
public static class VerificationClassPolicy
{
    public static (VerificationClass Class, ConfidenceIndicator Confidence) Classify(VerificationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (IsHighRisk(input))
        {
            var reason = DetermineHighRiskReason(input);
            var level = input.IdentityStatus == IdentityVerificationStatus.Failed
                     || input.BackgroundStatus == BackgroundCheckStatus.Fail
                ? ConfidenceLevel.High
                : ConfidenceLevel.Medium;
            return (VerificationClass.High, ConfidenceIndicator.Create(level, reason));
        }

        if (IsLowRisk(input))
        {
            return (VerificationClass.Low, ConfidenceIndicator.Create(ConfidenceLevel.High, "All signals pass"));
        }

        var mediumReason = DetermineMediumRiskReason(input);
        var mediumConfidence = input.BackgroundStatus == BackgroundCheckStatus.Review
            ? ConfidenceLevel.Medium
            : ConfidenceLevel.High;
        return (VerificationClass.Medium, ConfidenceIndicator.Create(mediumConfidence, mediumReason));
    }

    private static bool IsHighRisk(VerificationInput input) =>
        input.IdentityStatus is IdentityVerificationStatus.Failed or IdentityVerificationStatus.Pending
        || input.BackgroundStatus == BackgroundCheckStatus.Fail
        || input.InsuranceStatus == InsuranceStatus.None;

    private static bool IsLowRisk(VerificationInput input) =>
        input.IdentityStatus == IdentityVerificationStatus.Verified
        && input.BackgroundStatus == BackgroundCheckStatus.Pass
        && input.InsuranceStatus is InsuranceStatus.Active or InsuranceStatus.InstitutionBacked
        && input.ViolationCount == 0;

    private static string DetermineHighRiskReason(VerificationInput input)
    {
        if (input.IdentityStatus == IdentityVerificationStatus.Failed)
        {
            return "Identity verification failed";
        }

        if (input.IdentityStatus == IdentityVerificationStatus.Pending)
        {
            return "Identity verification pending";
        }

        if (input.BackgroundStatus == BackgroundCheckStatus.Fail)
        {
            return "Background check failed";
        }

        return "No insurance coverage";
    }

    private static string DetermineMediumRiskReason(VerificationInput input)
    {
        if (input.BackgroundStatus == BackgroundCheckStatus.Review)
        {
            return "Background check under review";
        }

        if (input.InsuranceStatus == InsuranceStatus.Inactive)
        {
            return "Insurance inactive";
        }

        if (input.ViolationCount > 0)
        {
            return $"Violation history ({input.ViolationCount} recorded)";
        }

        return "Standard verification level";
    }
}
