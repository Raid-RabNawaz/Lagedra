using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;

namespace Lagedra.Modules.ListingAndLocation.Domain.Policies;

public static class CancellationPolicyDefaults
{
    public static CancellationPolicy ForType(CancellationPolicyType type) => type switch
    {
        CancellationPolicyType.Flexible => CancellationPolicy.Create(type, freeCancellationDays: 7, partialRefundPercent: 50, partialRefundDays: 3, customTerms: null),
        CancellationPolicyType.Moderate => CancellationPolicy.Create(type, freeCancellationDays: 14, partialRefundPercent: 50, partialRefundDays: 7, customTerms: null),
        CancellationPolicyType.Strict => CancellationPolicy.Create(type, freeCancellationDays: 30, partialRefundPercent: 50, partialRefundDays: 14, customTerms: null),
        CancellationPolicyType.NonRefundable => CancellationPolicy.Create(type, freeCancellationDays: 0, partialRefundPercent: null, partialRefundDays: null, customTerms: null),
        CancellationPolicyType.Custom => CancellationPolicy.Create(type, freeCancellationDays: 14, partialRefundPercent: 50, partialRefundDays: 7, customTerms: null),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown cancellation policy type.")
    };
}
