namespace Lagedra.Modules.ActivationAndBilling.Domain.Policies;

public sealed record RefundBreakdown(
    long TenantRefundCents,
    long InsuranceRefundCents,
    string PolicyApplied);

public static class CancellationRefundCalculator
{
    public static RefundBreakdown Calculate(
        DateOnly checkIn,
        DateOnly today,
        long totalTenantPaymentCents,
        long insuranceFeeCents,
        int freeCancellationDays,
        int? partialRefundPercent,
        int? partialRefundDays)
    {
        var daysUntilCheckIn = checkIn.DayNumber - today.DayNumber;

        if (daysUntilCheckIn >= freeCancellationDays)
        {
            return new RefundBreakdown(
                totalTenantPaymentCents,
                insuranceFeeCents,
                $"Full refund (cancelled {daysUntilCheckIn} days before check-in, free cancellation within {freeCancellationDays} days)");
        }

        if (partialRefundPercent.HasValue && partialRefundDays.HasValue
            && daysUntilCheckIn >= partialRefundDays.Value)
        {
            var refund = totalTenantPaymentCents * partialRefundPercent.Value / 100;
            var insRefund = insuranceFeeCents * partialRefundPercent.Value / 100;
            return new RefundBreakdown(
                refund,
                insRefund,
                $"Partial refund {partialRefundPercent}% (cancelled {daysUntilCheckIn} days before check-in)");
        }

        return new RefundBreakdown(0, 0,
            $"No refund (cancelled {daysUntilCheckIn} days before check-in, policy requires {freeCancellationDays} days)");
    }
}
