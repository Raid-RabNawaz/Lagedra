using System;
using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Domain;

public class RentPeriodCalculatorTests
{
    // Reference stay from the product question that motivated this feature:
    // Aug 15 – Dec 15. Month 1 (Aug 15 – Sep 15) is paid at booking through
    // the platform, so direct-rent periods are Sep 15, Oct 15, Nov 15.
    private static readonly DateOnly CheckIn = new(2026, 8, 15);
    private static readonly DateOnly CheckOut = new(2026, 12, 15);

    [Fact]
    public void No_periods_due_before_the_first_monthly_anniversary()
    {
        var periods = RentPeriodCalculator.DuePeriods(CheckIn, CheckOut, new DateOnly(2026, 9, 14));

        periods.Should().BeEmpty();
    }

    [Fact]
    public void First_direct_rent_period_becomes_due_on_the_monthly_anniversary()
    {
        var periods = RentPeriodCalculator.DuePeriods(CheckIn, CheckOut, new DateOnly(2026, 9, 15));

        periods.Should().ContainSingle();
        periods[0].Start.Should().Be(new DateOnly(2026, 9, 15));
        periods[0].End.Should().Be(new DateOnly(2026, 10, 15));
    }

    [Fact]
    public void All_periods_of_the_stay_are_due_by_the_end()
    {
        var periods = RentPeriodCalculator.DuePeriods(CheckIn, CheckOut, new DateOnly(2026, 12, 31));

        periods.Should().HaveCount(3);
        periods[0].Start.Should().Be(new DateOnly(2026, 9, 15));
        periods[1].Start.Should().Be(new DateOnly(2026, 10, 15));
        periods[2].Start.Should().Be(new DateOnly(2026, 11, 15));
        periods[2].End.Should().Be(new DateOnly(2026, 12, 15));
    }

    [Fact]
    public void Partial_final_month_is_clipped_to_check_out()
    {
        // Aug 20 – Oct 5: month 1 covers Aug 20 – Sep 20; the final direct
        // period is Sep 20 – Oct 5.
        var periods = RentPeriodCalculator.DuePeriods(
            new DateOnly(2026, 8, 20), new DateOnly(2026, 10, 5), new DateOnly(2026, 10, 1));

        periods.Should().ContainSingle();
        periods[0].Start.Should().Be(new DateOnly(2026, 9, 20));
        periods[0].End.Should().Be(new DateOnly(2026, 10, 5));
    }

    [Fact]
    public void One_month_stay_has_no_direct_rent_periods()
    {
        // Entirely covered by the first platform-charged month.
        var periods = RentPeriodCalculator.DuePeriods(
            new DateOnly(2026, 8, 15), new DateOnly(2026, 9, 15), new DateOnly(2027, 1, 1));

        periods.Should().BeEmpty();
    }

    [Fact]
    public void Month_end_anniversaries_clamp_like_calendar_months()
    {
        // Check-in Jan 31: .NET clamps Jan 31 + 1 month to Feb 28 (2026 is
        // not a leap year), so the second period starts Feb 28.
        var periods = RentPeriodCalculator.DuePeriods(
            new DateOnly(2026, 1, 31), new DateOnly(2026, 5, 31), new DateOnly(2026, 3, 1));

        periods.Should().HaveCount(1);
        periods[0].Start.Should().Be(new DateOnly(2026, 2, 28));
        periods[0].End.Should().Be(new DateOnly(2026, 3, 31));
    }
}
