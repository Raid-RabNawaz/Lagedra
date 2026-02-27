using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;

public sealed class StayRange : ValueObject
{
    public int MinDays { get; }
    public int MaxDays { get; }

    public StayRange(int minDays, int maxDays)
    {
        if (minDays < 30)
        {
            throw new ArgumentOutOfRangeException(nameof(minDays), "Minimum stay must be at least 30 days.");
        }

        if (maxDays > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDays), "Maximum stay cannot exceed 180 days.");
        }

        if (minDays > maxDays)
        {
            throw new ArgumentException("Minimum stay cannot exceed maximum stay.", nameof(minDays));
        }

        MinDays = minDays;
        MaxDays = maxDays;
    }

    private StayRange() { }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return MinDays;
        yield return MaxDays;
    }
}
