using System;
using FluentAssertions;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

public class ChannelCalendarProjectionTests
{
    [Fact]
    public void Converts_inclusive_unavailable_nights_to_exclusive_checkout()
    {
        var ranges = ChannelCalendarProjection.ToHostBlockedRanges(
        [
            new ChannelDateBlock(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 5), Available: false),
            new ChannelDateBlock(new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 20), Available: true),
        ]);

        ranges.Should().ContainSingle();
        ranges[0].CheckIn.Should().Be(new DateOnly(2026, 10, 1));
        ranges[0].CheckOut.Should().Be(new DateOnly(2026, 10, 6));
    }

    [Fact]
    public void Skips_available_ranges()
    {
        var ranges = ChannelCalendarProjection.ToHostBlockedRanges(
        [
            new ChannelDateBlock(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30), Available: true),
        ]);

        ranges.Should().BeEmpty();
    }
}
