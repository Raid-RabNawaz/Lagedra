using Lagedra.Modules.ListingAndLocation.Domain.Enums;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Policies;

public static class RefundCalculator
{
    public static long CalculateRefund(
        CancellationPolicyType policyType,
        int freeCancellationDays,
        int? partialRefundPercent,
        int? partialRefundDays,
        DateOnly checkIn,
        DateOnly cancellationDate,
        long totalPaidCents)
    {
        var daysUntilCheckIn = checkIn.DayNumber - cancellationDate.DayNumber;

        if (daysUntilCheckIn <= 0)
        {
            return policyType == CancellationPolicyType.Flexible
                ? totalPaidCents / 2
                : 0;
        }

        return policyType switch
        {
            CancellationPolicyType.Flexible =>
                daysUntilCheckIn >= freeCancellationDays ? totalPaidCents : totalPaidCents / 2,

            CancellationPolicyType.Moderate =>
                daysUntilCheckIn >= freeCancellationDays ? totalPaidCents
                : partialRefundDays.HasValue && daysUntilCheckIn >= partialRefundDays.Value
                    ? totalPaidCents * (partialRefundPercent ?? 50) / 100
                    : 0,

            CancellationPolicyType.Strict =>
                daysUntilCheckIn >= freeCancellationDays
                    ? totalPaidCents * (partialRefundPercent ?? 50) / 100
                    : 0,

            CancellationPolicyType.NonRefundable => 0,

            CancellationPolicyType.Custom =>
                daysUntilCheckIn >= freeCancellationDays ? totalPaidCents
                : partialRefundDays.HasValue && daysUntilCheckIn >= partialRefundDays.Value
                    ? totalPaidCents * (partialRefundPercent ?? 0) / 100
                    : 0,

            _ => 0
        };
    }
}
