namespace Lagedra.SharedKernel.Settings;

/// <summary>
/// Recorded only when a covered booking ends before the scheduled check-out.
/// </summary>
public enum EvictionProtectionClaimStanding
{
    /// <summary>
    /// Early move-out was not a qualifying nonpayment or eviction event.
    /// Coverage ended with the booking. No claim applies.
    /// </summary>
    EndedNoClaim,

    /// <summary>
    /// Early move-out is connected to documented nonpayment and may still be
    /// reviewed under the Eviction Protection terms.
    /// </summary>
    ClaimReviewable,
}

/// <summary>
/// Guest-paid Lagedra Partner Eviction Protection. Separate from Truvi Stay
/// Protection. The fee is an admin setting; the coverage cap is fixed.
/// </summary>
public static class EvictionProtectionFee
{
    public const long DefaultFeeCents = 5_000;
    public const long CoverageCapCents = 1_000_000;
    public const string ClaimsEmail = "info@lagedra.com";

    public const string RefundBeforeCheckInTerm =
        "If the booking is cancelled before check-in, the Eviction Protection fee is refunded.";

    public const string NonQualifyingEarlyMoveOutTerm =
        "If the tenant moves out early for a reason that is not a qualifying nonpayment or eviction event, coverage ends when the booking ends and no claim applies.";

    public const string DocumentedNonpaymentEarlyMoveOutTerm =
        "If the early move-out is connected to documented nonpayment and meets the qualifying-event requirements, the claim may still be reviewed under the Eviction Protection terms.";

    public const string SettingDescription =
        "Fee charged to a guest with an active partner endorsement, once per booking. "
        + "Set to 0 to waive the fee. Coverage still applies. "
        + "Refunded only when the booking is cancelled before the check-in date. "
        + "An early move-out that is not a qualifying nonpayment or eviction event ends coverage with the booking. "
        + "An early move-out connected to documented nonpayment may still be reviewed.";

    public static long Normalize(long cents) => cents < 0 ? 0 : cents;

    /// <summary>
    /// Full fee back when cancellation is before the check-in date. Nothing
    /// on or after that date.
    /// </summary>
    public static long RefundableCents(DateOnly checkIn, DateOnly today, long feeCents)
    {
        if (feeCents <= 0)
        {
            return 0;
        }

        return checkIn.DayNumber - today.DayNumber > 0 ? feeCents : 0;
    }

    /// <summary>
    /// Early means the stay is ended before the scheduled check-out date.
    /// Ending on or after that date does not close a qualifying claim.
    /// </summary>
    public static EarlyMoveOutDecision DecideEarlyMoveOut(
        bool included,
        DateOnly scheduledCheckOut,
        DateOnly moveOutDate,
        bool? documentedNonpayment)
    {
        if (!included || moveOutDate >= scheduledCheckOut)
        {
            return new EarlyMoveOutDecision(false, null);
        }

        if (documentedNonpayment is null)
        {
            return new EarlyMoveOutDecision(true, null);
        }

        return new EarlyMoveOutDecision(
            false,
            documentedNonpayment.Value
                ? EvictionProtectionClaimStanding.ClaimReviewable
                : EvictionProtectionClaimStanding.EndedNoClaim);
    }
}

/// <summary>
/// <see cref="ReasonRequired"/> when a covered stay is ending early and the
/// caller has not said whether it is documented nonpayment.
/// <see cref="Standing"/> is stored only for that early move-out.
/// </summary>
public readonly record struct EarlyMoveOutDecision(
    bool ReasonRequired,
    EvictionProtectionClaimStanding? Standing);
