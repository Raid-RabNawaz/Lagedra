using Lagedra.Modules.VerificationAndRisk.Domain.Enums;

namespace Lagedra.Modules.VerificationAndRisk.Domain.Policies;

/// <summary>
/// Deposit band = f(VerificationClass, InsuranceState, JurisdictionCap).
/// Returns (lowCents, highCents) as a percentage of the jurisdiction cap.
/// </summary>
public static class DepositRecommendationPolicy
{
    public static (long LowCents, long HighCents) Recommend(
        VerificationClass verificationClass,
        InsuranceStatus insuranceStatus,
        long jurisdictionCapCents)
    {
        if (jurisdictionCapCents < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jurisdictionCapCents),
                jurisdictionCapCents,
                "Jurisdiction cap cannot be negative.");
        }

        var (lowPct, highPct) = (verificationClass, insuranceStatus) switch
        {
            (VerificationClass.Low, InsuranceStatus.Active or InsuranceStatus.InstitutionBacked)
                => (0.00m, 0.50m),
            (VerificationClass.Low, _)
                => (0.25m, 0.75m),
            (VerificationClass.Medium, InsuranceStatus.Active or InsuranceStatus.InstitutionBacked)
                => (0.25m, 0.75m),
            (VerificationClass.Medium, _)
                => (0.50m, 1.00m),
            _ => (0.75m, 1.00m)
        };

        var low = (long)(lowPct * jurisdictionCapCents);
        var high = (long)(highPct * jurisdictionCapCents);
        return (low, high);
    }
}
