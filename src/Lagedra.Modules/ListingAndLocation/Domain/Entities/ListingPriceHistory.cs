using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ListingAndLocation.Domain.Entities;

/// <summary>
/// Append-only price history for a listing. Records rent changes over time.
/// </summary>
public sealed class ListingPriceHistory : Entity<Guid>
{
    public Guid ListingId { get; private set; }
    public long MonthlyRentCents { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }

    private ListingPriceHistory() { }

    public static ListingPriceHistory Create(
        Guid listingId,
        long monthlyRentCents,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null)
    {
        if (monthlyRentCents <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(monthlyRentCents), "Monthly rent must be positive.");
        }

        return new ListingPriceHistory
        {
            Id = Guid.NewGuid(),
            ListingId = listingId,
            MonthlyRentCents = monthlyRentCents,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo
        };
    }

    public void Close(DateOnly effectiveTo)
    {
        if (effectiveTo < EffectiveFrom)
        {
            throw new ArgumentOutOfRangeException(nameof(effectiveTo), "EffectiveTo must be >= EffectiveFrom.");
        }

        EffectiveTo = effectiveTo;
    }
}
