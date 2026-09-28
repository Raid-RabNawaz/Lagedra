using FluentAssertions;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

public sealed class ChannelListingPriceInterpretationTests
{
    [Fact]
    public void Resolve_WithoutHostChoice_KeepsProviderReading()
    {
        var snapshot = Snapshot(monthlyCents: 860_700, nightlyCents: null, ChannelPriceBasis.Monthly);

        var rate = ChannelListingPriceInterpretation.Resolve(snapshot, savedBasis: null);

        rate.PriceBasis.Should().Be(ChannelPriceBasis.Monthly);
        rate.MonthlyRentCents.Should().Be(860_700);
        ChannelListingPriceInterpretation.SourceAmountCents(snapshot).Should().Be(860_700);
    }

    [Fact]
    public void Resolve_HostMonthly_DoesNotMultiplyANightlyFeedAmount()
    {
        var snapshot = Snapshot(monthlyCents: 634_860, nightlyCents: 21_162, ChannelPriceBasis.Nightly);

        var rate = ChannelListingPriceInterpretation.Resolve(snapshot, ChannelPmsPriceBasis.Monthly);

        rate.PriceBasis.Should().Be(ChannelPriceBasis.Monthly);
        rate.MonthlyRentCents.Should().Be(21_162);
        rate.NightlyRateCents.Should().BeNull();
    }

    [Fact]
    public void Resolve_HostNightly_MultipliesEvenWhenTheCapTreatedItAsMonthly()
    {
        var snapshot = Snapshot(monthlyCents: 860_700, nightlyCents: null, ChannelPriceBasis.Monthly);

        var rate = ChannelListingPriceInterpretation.Resolve(snapshot, ChannelPmsPriceBasis.Nightly);

        rate.PriceBasis.Should().Be(ChannelPriceBasis.Nightly);
        rate.NightlyRateCents.Should().Be(860_700);
        rate.MonthlyRentCents.Should().Be(860_700 * 30);
    }

    private static ChannelListingSnapshot Snapshot(
        long? monthlyCents,
        long? nightlyCents,
        ChannelPriceBasis basis)
        => new(
            ExternalListingId: "1",
            Title: "Reno",
            MonthlyRentCents: monthlyCents,
            NightlyRateCents: nightlyCents,
            PriceBasis: basis);
}
