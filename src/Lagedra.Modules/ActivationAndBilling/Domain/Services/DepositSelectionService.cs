using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Services;

/// <summary>
/// Pure mapping from a tenant's verification tier + a listing's predetermined
/// deposit amounts to the concrete deposit charged for a reservation, plus a
/// human-readable reason for the UI. When a tier-specific amount has not been
/// configured on the listing the maximum deposit is used as a safe fallback so
/// older listings (created before per-tier deposits existed) keep working.
/// </summary>
public static class DepositSelectionService
{
    public const string PartnerReason = "Partner guarantee applied";
    public const string BackgroundVerifiedReason = "Verified tenant discount applied";
    public const string UnverifiedReason = "Standard deposit (unverified tenant)";
    public const string FallbackReason = "Standard deposit (maximum)";

    public static DepositSelection Select(
        TenantVerificationTier tier,
        long maxDepositCents,
        long? unverifiedCents,
        long? backgroundVerifiedCents,
        long? partnerGuaranteedCents)
    {
        return tier switch
        {
            TenantVerificationTier.PartnerGuaranteed => partnerGuaranteedCents is { } partner
                ? new DepositSelection(partner, tier, PartnerReason)
                : new DepositSelection(maxDepositCents, tier, FallbackReason),

            TenantVerificationTier.BackgroundVerified => backgroundVerifiedCents is { } verified
                ? new DepositSelection(verified, tier, BackgroundVerifiedReason)
                : new DepositSelection(maxDepositCents, tier, FallbackReason),

            _ => unverifiedCents is { } unverified
                ? new DepositSelection(unverified, tier, UnverifiedReason)
                : new DepositSelection(maxDepositCents, tier, FallbackReason),
        };
    }
}

/// <summary>
/// The deposit chosen for a reservation: the amount in cents, the tier that
/// produced it, and a short reason shown to the tenant ("why this amount").
/// </summary>
public sealed record DepositSelection(
    long AmountCents,
    TenantVerificationTier Tier,
    string Reason);
