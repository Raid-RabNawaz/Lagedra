namespace Lagedra.SharedKernel.Insurance;

/// <summary>
/// Truvi Screen &amp; Protect wholesale recovery charged to the tenant at
/// booking. Nightly: $6 for the first 30 nights, $4 after that. Cancelled or
/// rejected screenings still cost Truvi $1, so refunds keep that remainder.
/// </summary>
public static class StayProtectionFee
{
    public const int DefaultFirstNights = 30;
    public const long DefaultFirstNightsFeeCents = 600;
    public const long DefaultAdditionalNightFeeCents = 400;
    public const long ScreeningFeeCents = 100;

    public static long ComputeNightlyFeeCents(
        int stayDurationDays,
        int firstNights = DefaultFirstNights,
        long firstNightsFeeCents = DefaultFirstNightsFeeCents,
        long additionalNightFeeCents = DefaultAdditionalNightFeeCents)
    {
        if (stayDurationDays <= 0 || firstNights <= 0)
        {
            return 0;
        }

        ArgumentOutOfRangeException.ThrowIfNegative(firstNightsFeeCents);
        ArgumentOutOfRangeException.ThrowIfNegative(additionalNightFeeCents);

        var billedFirst = Math.Min(stayDurationDays, firstNights);
        var additional = Math.Max(stayDurationDays - firstNights, 0);
        return checked((billedFirst * firstNightsFeeCents) + (additional * additionalNightFeeCents));
    }

    /// <summary>
    /// Caps a policy-based protection refund so at least
    /// <paramref name="retainCents"/> of the original fee stays with the
    /// platform (Truvi's $1 screening charge).
    /// </summary>
    public static long RefundableProtectionCents(
        long protectionFeeCents,
        long policyRefundCents,
        long retainCents = ScreeningFeeCents)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(protectionFeeCents);
        ArgumentOutOfRangeException.ThrowIfNegative(policyRefundCents);
        ArgumentOutOfRangeException.ThrowIfNegative(retainCents);

        var maxRefundable = Math.Max(0, protectionFeeCents - retainCents);
        return Math.Min(policyRefundCents, maxRefundable);
    }
}
