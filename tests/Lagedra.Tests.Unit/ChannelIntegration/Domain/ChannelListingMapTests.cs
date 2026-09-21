using System;
using FluentAssertions;
using Lagedra.Modules.ChannelIntegration.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Domain;

public class ChannelListingMapTests
{
    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow { get; set; } = new(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc);
    }

    [Fact]
    public void New_maps_do_not_sync_price_or_calendar()
    {
        var map = ChannelListingMap.Create(Guid.NewGuid(), "ext-1", "Loft", new FixedClock());

        map.SyncPriceFromPms.Should().BeFalse();
        map.SyncCalendarFromPms.Should().BeFalse();
    }

    [Fact]
    public void SetSyncPreferences_stores_each_flag_independently()
    {
        var clock = new FixedClock();
        var map = ChannelListingMap.Create(Guid.NewGuid(), "ext-1", "Loft", clock);

        map.SetSyncPreferences(syncPriceFromPms: true, syncCalendarFromPms: false, clock);

        map.SyncPriceFromPms.Should().BeTrue();
        map.SyncCalendarFromPms.Should().BeFalse();

        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        map.SetSyncPreferences(syncPriceFromPms: false, syncCalendarFromPms: true, clock);

        map.SyncPriceFromPms.Should().BeFalse();
        map.SyncCalendarFromPms.Should().BeTrue();
    }
}
