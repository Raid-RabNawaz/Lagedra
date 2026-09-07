using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Domain.Policies;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Domain;

public class CancellationRefundCalculatorTests
{
    private static readonly DateOnly CheckIn = new(2026, 10, 1);
    private static readonly DateOnly Today = new(2026, 9, 1);
    private const long RentAndDepositCents = 300_000;
    private const long StayProtectionCents = 18_000;

    [Fact]
    public void Full_refund_retains_one_dollar_of_stay_protection()
    {
        var refund = CancellationRefundCalculator.Calculate(
            CheckIn,
            Today,
            RentAndDepositCents,
            StayProtectionCents,
            freeCancellationDays: 14,
            partialRefundPercent: 50,
            partialRefundDays: 7);

        refund.TenantRefundCents.Should().Be(RentAndDepositCents);
        refund.InsuranceRefundCents.Should().Be(17_900);
        refund.PolicyApplied.Should().Contain("Full refund");
    }

    [Fact]
    public void Partial_refund_caps_stay_protection_at_policy_share()
    {
        var refund = CancellationRefundCalculator.Calculate(
            CheckIn,
            new DateOnly(2026, 9, 23),
            RentAndDepositCents,
            StayProtectionCents,
            freeCancellationDays: 14,
            partialRefundPercent: 50,
            partialRefundDays: 7);

        refund.TenantRefundCents.Should().Be(150_000);
        refund.InsuranceRefundCents.Should().Be(9_000);
        refund.PolicyApplied.Should().Contain("Partial refund");
    }

    [Fact]
    public void No_refund_window_returns_zero_stay_protection()
    {
        var refund = CancellationRefundCalculator.Calculate(
            CheckIn,
            new DateOnly(2026, 9, 28),
            RentAndDepositCents,
            StayProtectionCents,
            freeCancellationDays: 14,
            partialRefundPercent: 50,
            partialRefundDays: 7);

        refund.TenantRefundCents.Should().Be(0);
        refund.InsuranceRefundCents.Should().Be(0);
    }
}
