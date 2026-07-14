using Lagedra.Modules.VerificationAndRisk.Domain.Enums;

namespace Lagedra.Modules.VerificationAndRisk.Domain.Policies;

/// <summary>
/// Deposit band = f(VerificationClass, InsuranceState, JurisdictionCap, optional stay reputation).
/// Returns (lowCents, highCents) as a percentage of the jurisdiction cap.
///
/// <para>Sourcing of <see cref="InsuranceStatus.InstitutionBacked"/> (Phase 18 — Option A):
/// the band reduction for <c>InstitutionBacked</c> is awarded when EITHER (a) the tenant
/// holds a real third-party insurance binding flagged institution-backed, OR (b) at least
/// one verified partner organization has an Approved+unexpired <c>PartnerEndorsement</c>
/// for the tenant. The two paths produce the SAME band by deliberate design — there is no
/// "double discount" for being both endorsed and insured. The user-facing label
/// distinguishes the two via <c>ProtectionTier</c> on the read model, but pricing does not.</para>
///
/// <para>Stay reputation is a soft nudge only: with at least
/// <see cref="MinReviewsForReputationNudge"/> published reviews, a strong average (≥4.5)
/// trims the band by up to 5% of the jurisdiction cap, and a weak average (≤2.5) raises
/// it by the same amount. Reputation never changes <see cref="VerificationClass"/> itself.</para>
/// </summary>
public static class DepositRecommendationPolicy
{
    public const int MinReviewsForReputationNudge = 3;
    public const decimal ReputationNudgeFraction = 0.05m;

    public static (long LowCents, long HighCents) Recommend(
        VerificationClass verificationClass,
        InsuranceStatus insuranceStatus,
        long jurisdictionCapCents,
        double? reputationAverage = null,
        int reputationReviewCount = 0)
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

        var nudge = ResolveReputationNudgeCents(
            jurisdictionCapCents,
            reputationAverage,
            reputationReviewCount);
        if (nudge != 0)
        {
            low = Math.Clamp(low + nudge, 0, jurisdictionCapCents);
            high = Math.Clamp(high + nudge, low, jurisdictionCapCents);
        }

        return (low, high);
    }

    /// <summary>
    /// Negative cents lowers the deposit band (strong reputation);
    /// positive cents raises it (weak reputation).
    /// </summary>
    public static long ResolveReputationNudgeCents(
        long jurisdictionCapCents,
        double? reputationAverage,
        int reputationReviewCount)
    {
        if (reputationReviewCount < MinReviewsForReputationNudge
            || reputationAverage is not double avg
            || avg is < 1 or > 5)
        {
            return 0;
        }

        var magnitude = (long)(ReputationNudgeFraction * jurisdictionCapCents);
        if (avg >= 4.5)
        {
            return -magnitude;
        }

        if (avg <= 2.5)
        {
            return magnitude;
        }

        return 0;
    }
}
