using FluentAssertions;
using Lagedra.SharedKernel.Insurance;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class StayProtectionFeeTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(30, 18_000)]
    [InlineData(60, 30_000)]
    [InlineData(90, 42_000)]
    [InlineData(180, 78_000)]
    public void Nightly_schedule_matches_Truvi_rates(int stayDays, long expectedCents)
    {
        StayProtectionFee.ComputeNightlyFeeCents(stayDays).Should().Be(expectedCents);
    }

    [Fact]
    public void Full_refund_keeps_the_screening_dollar()
    {
        StayProtectionFee.RefundableProtectionCents(18_000, 18_000)
            .Should().Be(17_900);
    }

    [Fact]
    public void Partial_refund_is_capped_by_policy_and_retain()
    {
        StayProtectionFee.RefundableProtectionCents(18_000, 9_000)
            .Should().Be(9_000);
    }

    [Fact]
    public void Fee_at_or_below_screening_charge_is_not_refundable()
    {
        StayProtectionFee.RefundableProtectionCents(100, 100).Should().Be(0);
        StayProtectionFee.RefundableProtectionCents(0, 0).Should().Be(0);
    }
}
