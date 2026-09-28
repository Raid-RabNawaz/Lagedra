using FluentAssertions;
using Lagedra.SharedKernel.Settings;
using Xunit;

namespace Lagedra.Tests.Unit.SharedKernel;

public class EvictionProtectionFeeTests
{
    private static readonly DateOnly CheckOut = new(2026, 10, 15);

    [Fact]
    public void Early_move_out_without_a_reason_must_be_answered()
    {
        var decision = EvictionProtectionFee.DecideEarlyMoveOut(
            included: true,
            CheckOut,
            moveOutDate: new DateOnly(2026, 10, 1),
            documentedNonpayment: null);

        decision.ReasonRequired.Should().BeTrue();
        decision.Standing.Should().BeNull();
    }

    [Fact]
    public void Non_qualifying_early_move_out_ends_coverage()
    {
        var decision = EvictionProtectionFee.DecideEarlyMoveOut(
            included: true,
            CheckOut,
            moveOutDate: new DateOnly(2026, 10, 14),
            documentedNonpayment: false);

        decision.ReasonRequired.Should().BeFalse();
        decision.Standing.Should().Be(EvictionProtectionClaimStanding.EndedNoClaim);
    }

    [Fact]
    public void Documented_nonpayment_early_move_out_stays_reviewable()
    {
        var decision = EvictionProtectionFee.DecideEarlyMoveOut(
            included: true,
            CheckOut,
            moveOutDate: new DateOnly(2026, 9, 1),
            documentedNonpayment: true);

        decision.ReasonRequired.Should().BeFalse();
        decision.Standing.Should().Be(EvictionProtectionClaimStanding.ClaimReviewable);
    }

    [Theory]
    [InlineData(2026, 10, 15)]
    [InlineData(2026, 10, 16)]
    public void Ending_on_or_after_check_out_does_not_close_a_claim(int year, int month, int day)
    {
        var decision = EvictionProtectionFee.DecideEarlyMoveOut(
            included: true,
            CheckOut,
            moveOutDate: new DateOnly(year, month, day),
            documentedNonpayment: null);

        decision.ReasonRequired.Should().BeFalse();
        decision.Standing.Should().BeNull();
    }

    [Fact]
    public void A_booking_without_coverage_does_not_ask_for_a_reason()
    {
        var decision = EvictionProtectionFee.DecideEarlyMoveOut(
            included: false,
            CheckOut,
            moveOutDate: new DateOnly(2026, 9, 1),
            documentedNonpayment: null);

        decision.ReasonRequired.Should().BeFalse();
        decision.Standing.Should().BeNull();
    }
}
