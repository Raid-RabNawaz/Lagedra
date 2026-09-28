namespace Lagedra.Infrastructure.External.Channels;

/// <summary>
/// Turns a PMS listing amount into Lagedra monthly rent.
/// </summary>
/// <remarks>
/// Connected PMS listing feeds (Hostaway, Guesty, OwnerRez, Smoobu, Hosthub)
/// do not send a monthly-vs-nightly flag. Hostaway <c>price</c> and Guesty
/// <c>basePrice</c> are documented as a default nightly rate, but hosts who
/// let monthly enter that field (the Hostaway Reno case: $8,607 stored as
/// the base price) were multiplied by 30 again. Length-of-stay factors such
/// as Hostaway <c>monthlyDiscount</c> and Guesty <c>monthlyPriceFactor</c>
/// are percentages, not a monthly dollar amount, and are not applied here.
/// </remarks>
public static class ChannelRateNormalizer
{
    /// <summary>
    /// Nights used when a nightly PMS rate is converted to a monthly rent.
    /// </summary>
    public const int NightsPerMonth = 30;

    /// <summary>
    /// Converted monthly rent above this ($40,000) is treated as evidence that
    /// an unlabeled PMS amount was already monthly. Comparison is strict:
    /// a product equal to this cap stays a nightly × <see cref="NightsPerMonth"/> rate.
    /// </summary>
    public const long AbsurdMonthlyRentCents = 4_000_000;

    /// <summary>Converts a positive major-unit amount to cents. Zero and missing stay null.</summary>
    public static long? ToCents(decimal? amount)
    {
        if (amount is not > 0)
        {
            return null;
        }

        return (long)Math.Round(amount.Value * 100m, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Maps <paramref name="amountCents"/> using <paramref name="explicitBasis"/>.
    /// Unlabeled amounts are nightly × <see cref="NightsPerMonth"/> unless that
    /// product exceeds <see cref="AbsurdMonthlyRentCents"/>, in which case the
    /// amount is already the monthly rent.
    /// </summary>
    public static ChannelNormalizedRate Normalize(
        long? amountCents,
        ChannelPriceBasis explicitBasis = ChannelPriceBasis.Unknown)
    {
        if (amountCents is not > 0)
        {
            return ChannelNormalizedRate.None;
        }

        var amount = amountCents.Value;
        if (explicitBasis == ChannelPriceBasis.Monthly)
        {
            return new ChannelNormalizedRate(amount, null, ChannelPriceBasis.Monthly);
        }

        if (!TryScale(amount, NightsPerMonth, out var asMonthly))
        {
            return new ChannelNormalizedRate(amount, null, ChannelPriceBasis.Monthly);
        }

        if (explicitBasis == ChannelPriceBasis.Nightly)
        {
            return new ChannelNormalizedRate(asMonthly, amount, ChannelPriceBasis.Nightly);
        }

        if (asMonthly > AbsurdMonthlyRentCents)
        {
            return new ChannelNormalizedRate(amount, null, ChannelPriceBasis.Monthly);
        }

        return new ChannelNormalizedRate(asMonthly, amount, ChannelPriceBasis.Nightly);
    }

    /// <summary>
    /// True when a host should confirm whether <paramref name="amountCents"/> is
    /// already monthly. A nightly reading whose × <see cref="NightsPerMonth"/>
    /// product exceeds <see cref="AbsurdMonthlyRentCents"/> is the odd case.
    /// Ordinary nightly amounts do not need a prompt.
    /// </summary>
    public static bool NeedsPricePeriodConfirmation(long? amountCents)
    {
        if (amountCents is not > 0)
        {
            return false;
        }

        if (!TryScale(amountCents.Value, NightsPerMonth, out var asMonthly))
        {
            return true;
        }

        return asMonthly > AbsurdMonthlyRentCents;
    }

    /// <summary>
    /// True when a content sync should write <paramref name="normalizedMonthlyRentCents"/>
    /// onto a listing that already has <paramref name="storedMonthlyRentCents"/>.
    /// Price sync always applies a change. Otherwise a stored rent that is still
    /// the old unlabeled-amount ×30 or ×31 inflation is corrected when the PMS
    /// amount was interpreted as already monthly.
    /// </summary>
    public static bool ShouldApplyNormalizedRent(
        long storedMonthlyRentCents,
        long normalizedMonthlyRentCents,
        bool syncPriceFromPms,
        bool pmsAmountIsMonthlyRent)
    {
        if (normalizedMonthlyRentCents <= 0)
        {
            return false;
        }

        if (syncPriceFromPms)
        {
            return storedMonthlyRentCents != normalizedMonthlyRentCents;
        }

        return pmsAmountIsMonthlyRent
            && IsInflatedMonthlyFingerprint(storedMonthlyRentCents, normalizedMonthlyRentCents);
    }

    /// <summary>
    /// True when <paramref name="storedMonthlyRentCents"/> is approximately
    /// <paramref name="correctedMonthlyRentCents"/> × 30 or × 31 — the fingerprint
    /// of treating an already-monthly PMS amount as a nightly rate.
    /// </summary>
    public static bool IsInflatedMonthlyFingerprint(
        long storedMonthlyRentCents,
        long correctedMonthlyRentCents)
    {
        if (storedMonthlyRentCents <= 0
            || correctedMonthlyRentCents <= 0
            || storedMonthlyRentCents == correctedMonthlyRentCents)
        {
            return false;
        }

        return IsNear(storedMonthlyRentCents, correctedMonthlyRentCents, multiplier: 30)
            || IsNear(storedMonthlyRentCents, correctedMonthlyRentCents, multiplier: 31);
    }

    private static bool IsNear(long storedCents, long monthlyCents, int multiplier)
    {
        if (!TryScale(monthlyCents, multiplier, out var expected))
        {
            return false;
        }

        var delta = Math.Abs(storedCents - expected);
        var tolerance = Math.Max(1L, expected / 200);
        return delta <= tolerance;
    }

    private static bool TryScale(long amount, int factor, out long scaled)
    {
        if (amount > long.MaxValue / factor)
        {
            scaled = 0;
            return false;
        }

        scaled = amount * factor;
        return true;
    }
}

/// <summary>Monthly and nightly cents produced by <see cref="ChannelRateNormalizer"/>.</summary>
public readonly record struct ChannelNormalizedRate(
    long? MonthlyRentCents,
    long? NightlyRateCents,
    ChannelPriceBasis PriceBasis)
{
    public static ChannelNormalizedRate None { get; } = new(null, null, ChannelPriceBasis.Unknown);
}
