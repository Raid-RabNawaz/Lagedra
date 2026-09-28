using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Domain;

public class AvailabilityServiceTests
{
    private static readonly DateOnly SearchStart = new(2026, 1, 1);
    private static readonly DateOnly SearchEnd = new(2026, 3, 20);

    [Fact]
    public void Booking_in_the_middle_of_a_search_does_not_cover_the_window()
    {
        AvailabilityService.CoversEntireWindow(
            new DateOnly(2026, 1, 10),
            new DateOnly(2026, 1, 31),
            SearchStart,
            SearchEnd).Should().BeFalse();
    }

    [Fact]
    public void Booking_that_spans_the_whole_search_covers_the_window()
    {
        AvailabilityService.CoversEntireWindow(
            new DateOnly(2025, 12, 1),
            new DateOnly(2026, 4, 1),
            SearchStart,
            SearchEnd).Should().BeTrue();
    }

    [Fact]
    public void Booking_that_starts_on_the_search_check_in_and_ends_on_check_out_covers_the_window()
    {
        AvailabilityService.CoversEntireWindow(
            SearchStart,
            SearchEnd,
            SearchStart,
            SearchEnd).Should().BeTrue();
    }

    [Fact]
    public void Booking_that_ends_on_the_search_check_in_does_not_cover_the_window()
    {
        AvailabilityService.CoversEntireWindow(
            new DateOnly(2025, 12, 1),
            SearchStart,
            SearchStart,
            SearchEnd).Should().BeFalse();
    }
}
