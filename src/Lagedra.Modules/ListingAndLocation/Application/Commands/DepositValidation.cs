namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

/// <summary>
/// Shared predicates for validating predetermined per-verification-tier deposit
/// amounts on listing create/update commands. Mirrors the invariants enforced
/// by <c>Listing.SetVerificationDeposits</c> so callers get a friendly 400
/// instead of a domain exception (500).
/// </summary>
internal static class DepositValidation
{
    /// <summary>
    /// True when <paramref name="value"/> is unset, or in the inclusive range
    /// <c>[0, max]</c>.
    /// </summary>
    public static bool IsWithinCap(long? value, long max) =>
        !value.HasValue || (value.Value >= 0 && value.Value <= max);

    /// <summary>
    /// True when the supplied tier amounts satisfy
    /// <c>partner ≤ background ≤ unverified</c> (more trust ⇒ lower deposit).
    /// Missing tiers are skipped so a partial configuration is still valid.
    /// </summary>
    public static bool IsOrdered(long? partner, long? background, long? unverified)
    {
        if (partner.HasValue && background.HasValue && partner.Value > background.Value)
        {
            return false;
        }

        if (background.HasValue && unverified.HasValue && background.Value > unverified.Value)
        {
            return false;
        }

        if (partner.HasValue && unverified.HasValue && partner.Value > unverified.Value)
        {
            return false;
        }

        return true;
    }
}
