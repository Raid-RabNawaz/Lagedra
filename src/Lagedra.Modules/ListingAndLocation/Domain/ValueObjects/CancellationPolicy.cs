using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;

public sealed class CancellationPolicy : ValueObject
{
    public CancellationPolicyType Type { get; private set; }
    public int FreeCancellationDays { get; private set; }
    public int? PartialRefundPercent { get; private set; }
    public int? PartialRefundDays { get; private set; }
    public string? CustomTerms { get; private set; }

    private CancellationPolicy() { }

    public static CancellationPolicy Create(
        CancellationPolicyType type,
        int freeCancellationDays,
        int? partialRefundPercent,
        int? partialRefundDays,
        string? customTerms)
    {
        if (freeCancellationDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(freeCancellationDays), "Free cancellation days cannot be negative.");
        }

        if (partialRefundPercent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(partialRefundPercent), "Partial refund percent must be between 0 and 100.");
        }

        return new CancellationPolicy
        {
            Type = type,
            FreeCancellationDays = freeCancellationDays,
            PartialRefundPercent = partialRefundPercent,
            PartialRefundDays = partialRefundDays,
            CustomTerms = customTerms?.Length > 2000 ? customTerms[..2000] : customTerms
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return FreeCancellationDays;
        yield return PartialRefundPercent;
        yield return PartialRefundDays;
        yield return CustomTerms;
    }
}
