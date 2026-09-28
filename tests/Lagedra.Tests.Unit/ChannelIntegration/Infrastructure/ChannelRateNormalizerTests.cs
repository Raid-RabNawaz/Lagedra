using FluentAssertions;
using Lagedra.Infrastructure.External.Channels;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

public sealed class ChannelRateNormalizerTests
{
    [Fact]
    public void Normalize_NightlyAmount_MultipliesByThirty()
    {
        var rate = ChannelRateNormalizer.Normalize(18_000); // $180 / night

        rate.PriceBasis.Should().Be(ChannelPriceBasis.Nightly);
        rate.NightlyRateCents.Should().Be(18_000);
        rate.MonthlyRentCents.Should().Be(540_000);
    }

    [Fact]
    public void Normalize_UnlabeledAmountAboveMonthlyCap_TreatsAmountAsMonthly()
    {
        var rate = ChannelRateNormalizer.Normalize(860_700); // Hostaway $8,607 base price

        rate.PriceBasis.Should().Be(ChannelPriceBasis.Monthly);
        rate.MonthlyRentCents.Should().Be(860_700);
        rate.NightlyRateCents.Should().BeNull();
    }

    [Fact]
    public void Normalize_MonthlyCapBoundary_StaysNightlyUntilProductExceedsCap()
    {
        var under = ChannelRateNormalizer.AbsurdMonthlyRentCents / ChannelRateNormalizer.NightsPerMonth;
        var atCap = ChannelRateNormalizer.Normalize(under);
        atCap.PriceBasis.Should().Be(ChannelPriceBasis.Nightly);
        atCap.MonthlyRentCents.Should().Be(under * ChannelRateNormalizer.NightsPerMonth);
        atCap.MonthlyRentCents.Should().BeLessThanOrEqualTo(ChannelRateNormalizer.AbsurdMonthlyRentCents);

        var over = ChannelRateNormalizer.Normalize(under + 1);
        over.PriceBasis.Should().Be(ChannelPriceBasis.Monthly);
        over.MonthlyRentCents.Should().Be(under + 1);
        over.NightlyRateCents.Should().BeNull();
    }

    [Fact]
    public void Normalize_ExplicitNightly_AlwaysMultipliesEvenAboveTheCap()
    {
        var rate = ChannelRateNormalizer.Normalize(860_700, ChannelPriceBasis.Nightly);

        rate.PriceBasis.Should().Be(ChannelPriceBasis.Nightly);
        rate.NightlyRateCents.Should().Be(860_700);
        rate.MonthlyRentCents.Should().Be(860_700 * 30);
    }

    [Theory]
    [InlineData(18_950, 568_500)] // OwnerRez $189.50
    [InlineData(8_500, 255_000)] // Smoobu $85
    [InlineData(12_245, 367_350)] // Hosthub $122.45
    public void Normalize_ExplicitNightlyFixtures_MatchExistingProviderMappings(int nightlyCents, int monthlyCents)
    {
        var rate = ChannelRateNormalizer.Normalize(nightlyCents, ChannelPriceBasis.Nightly);

        rate.PriceBasis.Should().Be(ChannelPriceBasis.Nightly);
        rate.NightlyRateCents.Should().Be(nightlyCents);
        rate.MonthlyRentCents.Should().Be(monthlyCents);
    }

    [Fact]
    public void Normalize_ExplicitMonthly_DoesNotInventANightlyRate()
    {
        var rate = ChannelRateNormalizer.Normalize(250_000, ChannelPriceBasis.Monthly);

        rate.PriceBasis.Should().Be(ChannelPriceBasis.Monthly);
        rate.MonthlyRentCents.Should().Be(250_000);
        rate.NightlyRateCents.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Normalize_MissingOrNonPositive_ReturnsNone(long? amountCents)
    {
        var rate = ChannelRateNormalizer.Normalize(amountCents);

        rate.Should().Be(ChannelNormalizedRate.None);
        rate.MonthlyRentCents.Should().BeNull();
        rate.NightlyRateCents.Should().BeNull();
        rate.PriceBasis.Should().Be(ChannelPriceBasis.Unknown);
    }

    [Fact]
    public void ToCents_RoundsPositiveMajorUnits()
    {
        ChannelRateNormalizer.ToCents(211.62m).Should().Be(21_162);
        ChannelRateNormalizer.ToCents(8607m).Should().Be(860_700);
        ChannelRateNormalizer.ToCents(0m).Should().BeNull();
        ChannelRateNormalizer.ToCents(null).Should().BeNull();
    }

    [Fact]
    public void IsInflatedMonthlyFingerprint_MatchesTimes30AndTimes31Only()
    {
        const long monthly = 860_700;

        ChannelRateNormalizer.IsInflatedMonthlyFingerprint(monthly * 30, monthly).Should().BeTrue();
        ChannelRateNormalizer.IsInflatedMonthlyFingerprint(monthly * 31, monthly).Should().BeTrue();
        ChannelRateNormalizer.IsInflatedMonthlyFingerprint(monthly, monthly).Should().BeFalse();
        ChannelRateNormalizer.IsInflatedMonthlyFingerprint(200_000, monthly).Should().BeFalse();

        var expected = monthly * 30;
        ChannelRateNormalizer.IsInflatedMonthlyFingerprint(expected + (expected / 400), monthly).Should().BeTrue();
        ChannelRateNormalizer.IsInflatedMonthlyFingerprint(expected + (expected / 50), monthly).Should().BeFalse();
    }

    [Fact]
    public void NeedsPricePeriodConfirmation_OnlyWhenNightlyReadingExceedsMonthlyCap()
    {
        ChannelRateNormalizer.NeedsPricePeriodConfirmation(18_000).Should().BeFalse();
        ChannelRateNormalizer.NeedsPricePeriodConfirmation(null).Should().BeFalse();
        ChannelRateNormalizer.NeedsPricePeriodConfirmation(0).Should().BeFalse();

        var atCap = ChannelRateNormalizer.AbsurdMonthlyRentCents / ChannelRateNormalizer.NightsPerMonth;
        ChannelRateNormalizer.NeedsPricePeriodConfirmation(atCap).Should().BeFalse();
        ChannelRateNormalizer.NeedsPricePeriodConfirmation(atCap + 1).Should().BeTrue();
        ChannelRateNormalizer.NeedsPricePeriodConfirmation(860_700).Should().BeTrue();
    }

    [Fact]
    public void ShouldApplyNormalizedRent_CorrectsInflationOnlyWhenAmountIsMonthly()
    {
        const long monthly = 860_700;

        ChannelRateNormalizer.ShouldApplyNormalizedRent(
            monthly * 30, monthly, syncPriceFromPms: false, pmsAmountIsMonthlyRent: true)
            .Should().BeTrue();

        ChannelRateNormalizer.ShouldApplyNormalizedRent(
            monthly * 30, monthly, syncPriceFromPms: false, pmsAmountIsMonthlyRent: false)
            .Should().BeFalse();

        ChannelRateNormalizer.ShouldApplyNormalizedRent(
            200_000, monthly, syncPriceFromPms: true, pmsAmountIsMonthlyRent: false)
            .Should().BeTrue();

        ChannelRateNormalizer.ShouldApplyNormalizedRent(
            monthly, monthly, syncPriceFromPms: true, pmsAmountIsMonthlyRent: true)
            .Should().BeFalse();
    }
}
