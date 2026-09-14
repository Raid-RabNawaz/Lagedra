using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Services;

/// <summary>
/// Pure mapping from a tenant's trust tier + a listing's predetermined
/// deposit amounts to the concrete deposit charged for a reservation.
/// Unverified and Verified share the standard band; Trusted uses the
/// former background-verified column; Elite uses the former partner column.
/// </summary>
public static class DepositSelectionService
{
    public const string EliteReason = "Elite tenant discount applied";
    public const string TrustedReason = "Trusted tenant discount applied";
    public const string UnverifiedReason = "Standard deposit";
    public const string FallbackReason = "Standard deposit (maximum)";

    /// <summary>Legacy alias kept so older snapshots still match in tests.</summary>
    public const string PartnerReason = EliteReason;

    /// <summary>Legacy alias kept so older snapshots still match in tests.</summary>
    public const string BackgroundVerifiedReason = TrustedReason;

    public static DepositSelection Select(
        TenantVerificationTier tier,
        long maxDepositCents,
        long? unverifiedCents,
        long? backgroundVerifiedCents,
        long? partnerGuaranteedCents)
    {
        return tier switch
        {
            TenantVerificationTier.Elite => partnerGuaranteedCents is { } elite
                ? new DepositSelection(elite, tier, EliteReason)
                : new DepositSelection(maxDepositCents, tier, FallbackReason),

            TenantVerificationTier.Trusted => backgroundVerifiedCents is { } trusted
                ? new DepositSelection(trusted, tier, TrustedReason)
                : new DepositSelection(maxDepositCents, tier, FallbackReason),

            TenantVerificationTier.Blocklisted =>
                new DepositSelection(maxDepositCents, tier, FallbackReason),

            _ => unverifiedCents is { } standard
                ? new DepositSelection(standard, tier, UnverifiedReason)
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
