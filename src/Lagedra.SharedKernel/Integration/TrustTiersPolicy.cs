using Lagedra.SharedKernel.Settings;

namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Inputs for <see cref="TrustTiersPolicy"/>. Screening is one optional
/// factor — never sufficient alone, never enough for Elite, never Blocklist.
/// </summary>
public sealed record TrustTierSignals(
    bool IsIdentityVerified,
    bool IsBlocklisted,
    bool HasActivePartnerEndorsement,
    int CompletedStayCount,
    int HostToGuestReviewCount,
    double HostToGuestAverage,
    int OpenViolationCount,
    bool HasUnresolvedArbitration,
    bool HasRecentScreeningPass);

/// <summary>
/// Rules-based trust classification. Keep all "what is Trusted?" logic here
/// so webhooks and UI cannot special-case a vendor.
/// </summary>
public static class TrustTiersPolicy
{
    public static TenantVerificationTier Classify(
        TrustTierSignals signals,
        TrustTiersOptions options)
    {
        ArgumentNullException.ThrowIfNull(signals);
        ArgumentNullException.ThrowIfNull(options);

        if (signals.IsBlocklisted)
        {
            return TenantVerificationTier.Blocklisted;
        }

        if (!signals.IsIdentityVerified)
        {
            return TenantVerificationTier.Unverified;
        }

        var gatesPass = signals.OpenViolationCount == 0
            && !signals.HasUnresolvedArbitration;

        var factorCount = 0;
        if (signals.CompletedStayCount >= options.TrustedMinStays)
        {
            factorCount++;
        }

        if (signals.HostToGuestReviewCount >= options.TrustedMinReviews
            && signals.HostToGuestAverage >= options.TrustedMinRating)
        {
            factorCount++;
        }

        if (signals.HasRecentScreeningPass)
        {
            factorCount++;
        }

        if (!gatesPass || factorCount < options.TrustedMinFactors)
        {
            return TenantVerificationTier.Verified;
        }

        var eliteHistory = signals.CompletedStayCount >= options.EliteMinStays
            && signals.HostToGuestReviewCount >= options.EliteMinReviews
            && signals.HostToGuestAverage >= options.EliteMinRating;

        if (eliteHistory && signals.HasActivePartnerEndorsement)
        {
            return TenantVerificationTier.Elite;
        }

        return TenantVerificationTier.Trusted;
    }
}
