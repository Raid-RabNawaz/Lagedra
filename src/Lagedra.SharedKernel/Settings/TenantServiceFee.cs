namespace Lagedra.SharedKernel.Settings;

/// <summary>
/// Tenant platform service fee. Charged to the tenant at checkout (on top of
/// rent + deposit + insurance) and kept by the platform.
///
/// The fee can be configured two ways, chosen by
/// <see cref="PlatformSettingKeys.TenantServiceFeeUseFlat"/>:
/// <list type="bullet">
///   <item><description><b>Percentage</b> (default): basis points of the first
///     month's rent stored in <see cref="PlatformSettingKeys.TenantServiceFeeBps"/>
///     (10,000 bps = 100%).</description></item>
///   <item><description><b>Flat</b>: a fixed amount in cents stored in
///     <see cref="PlatformSettingKeys.TenantServiceFeeFlatCents"/>.</description></item>
/// </list>
/// A value of 0 in the active mode disables the fee.
/// </summary>
public static class TenantServiceFee
{
    /// <summary>
    /// Resolves the service fee in cents for the active mode. When
    /// <paramref name="useFlat"/> is true the flat amount is used; otherwise the
    /// percentage of <paramref name="rentBaseCents"/> is computed.
    /// </summary>
    public static long Compute(long rentBaseCents, bool useFlat, long flatCents, long bps)
        => useFlat
            ? (flatCents > 0 ? flatCents : 0)
            : ComputeCents(rentBaseCents, bps);

    /// <summary>
    /// Computes the percentage service fee in cents for a given rent base and
    /// rate in basis points. Returns 0 when the rate or base is non-positive.
    /// Rounds to the nearest cent (half away from zero) so the figure matches
    /// what Stripe charges.
    /// </summary>
    public static long ComputeCents(long rentBaseCents, long bps)
    {
        if (bps <= 0 || rentBaseCents <= 0)
        {
            return 0;
        }

        return (long)Math.Round(rentBaseCents * (decimal)bps / 10_000m, MidpointRounding.AwayFromZero);
    }
}
