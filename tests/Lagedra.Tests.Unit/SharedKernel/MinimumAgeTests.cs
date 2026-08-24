using System;
using FluentAssertions;
using Lagedra.SharedKernel.Time;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class MinimumAgeTests
{
    private static readonly DateTime Now = new(2026, 8, 12, 10, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Exactly_18_today_counts_as_adult()
    {
        var dob = new DateOnly(2008, 8, 12);

        MinimumAge.IsAtLeast(MinimumAge.AdultYears, dob, Now).Should().BeTrue();
    }

    [Fact]
    public void Turning_18_tomorrow_is_not_adult_yet()
    {
        var dob = new DateOnly(2008, 8, 13);

        MinimumAge.IsAtLeast(MinimumAge.AdultYears, dob, Now).Should().BeFalse();
    }

    [Fact]
    public void Older_than_18_is_adult()
    {
        var dob = new DateOnly(1990, 1, 1);

        MinimumAge.IsAtLeast(MinimumAge.AdultYears, dob, Now).Should().BeTrue();
    }

    [Fact]
    public void DateTime_overload_ignores_time_of_day_and_kind()
    {
        var dob = new DateTime(2008, 8, 12, 23, 59, 0, DateTimeKind.Unspecified);

        MinimumAge.IsAtLeast(MinimumAge.AdultYears, dob, Now).Should().BeTrue();
    }

    [Fact]
    public void Feb_29_birthday_counts_on_mar_1_of_non_leap_years()
    {
        // Born Feb 29, 2008: in the non-leap year 2026 the 18th birthday is
        // treated as reached on Mar 1 (conservative reading), not Feb 28.
        var dob = new DateOnly(2008, 2, 29);
        var feb28 = new DateTime(2026, 2, 28, 12, 0, 0, DateTimeKind.Utc);
        var mar1 = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

        MinimumAge.IsAtLeast(MinimumAge.AdultYears, dob, feb28).Should().BeFalse();
        MinimumAge.IsAtLeast(MinimumAge.AdultYears, dob, mar1).Should().BeTrue();
    }
}
