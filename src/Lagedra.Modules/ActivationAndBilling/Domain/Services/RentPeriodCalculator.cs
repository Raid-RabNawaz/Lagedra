namespace Lagedra.Modules.ActivationAndBilling.Domain.Services;

/// <summary>A single monthly rent period within a stay.</summary>
public readonly record struct RentPeriod(DateOnly Start, DateOnly End);

/// <summary>
/// Computes the monthly rent periods of a stay that are paid directly to the
/// host. The first month (check-in to check-in + 1 month) is charged through
/// the platform at booking, so direct-rent periods start with month 2. Each
/// period begins on the stay's monthly anniversary and the last one is
/// clipped to the check-out date.
/// </summary>
public static class RentPeriodCalculator
{
    /// <summary>
    /// Periods whose start date has been reached as of <paramref name="today"/>
    /// (i.e. the rent for that period is due and a check-in can be asked).
    /// </summary>
    public static IReadOnlyList<RentPeriod> DuePeriods(DateOnly checkIn, DateOnly checkOut, DateOnly today)
    {
        var periods = new List<RentPeriod>();

        for (var month = 1; ; month++)
        {
            var start = checkIn.AddMonths(month);
            if (start >= checkOut || start > today)
            {
                break;
            }

            var end = checkIn.AddMonths(month + 1);
            if (end > checkOut)
            {
                end = checkOut;
            }

            periods.Add(new RentPeriod(start, end));
        }

        return periods;
    }
}
