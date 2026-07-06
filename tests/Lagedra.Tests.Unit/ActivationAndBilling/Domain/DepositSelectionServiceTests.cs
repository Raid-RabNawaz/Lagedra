using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Lagedra.SharedKernel.Integration;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Domain;

public class DepositSelectionServiceTests
{
    private const long Max = 500_000;
    private const long Unverified = 300_000;
    private const long Background = 200_000;
    private const long Partner = 100_000;

    [Fact]
    public void Partner_tier_selects_partner_deposit_with_reason()
    {
        var result = DepositSelectionService.Select(
            TenantVerificationTier.PartnerGuaranteed, Max, Unverified, Background, Partner);

        result.AmountCents.Should().Be(Partner);
        result.Tier.Should().Be(TenantVerificationTier.PartnerGuaranteed);
        result.Reason.Should().Be(DepositSelectionService.PartnerReason);
    }

    [Fact]
    public void Background_tier_selects_background_deposit_with_reason()
    {
        var result = DepositSelectionService.Select(
            TenantVerificationTier.BackgroundVerified, Max, Unverified, Background, Partner);

        result.AmountCents.Should().Be(Background);
        result.Reason.Should().Be(DepositSelectionService.BackgroundVerifiedReason);
    }

    [Fact]
    public void Unverified_tier_selects_unverified_deposit_with_reason()
    {
        var result = DepositSelectionService.Select(
            TenantVerificationTier.Unverified, Max, Unverified, Background, Partner);

        result.AmountCents.Should().Be(Unverified);
        result.Reason.Should().Be(DepositSelectionService.UnverifiedReason);
    }

    [Theory]
    [InlineData(TenantVerificationTier.PartnerGuaranteed)]
    [InlineData(TenantVerificationTier.BackgroundVerified)]
    [InlineData(TenantVerificationTier.Unverified)]
    public void Missing_tier_amount_falls_back_to_max_deposit(TenantVerificationTier tier)
    {
        // No per-tier amounts configured (legacy listing) -> safe fallback to max.
        var result = DepositSelectionService.Select(tier, Max, null, null, null);

        result.AmountCents.Should().Be(Max);
        result.Tier.Should().Be(tier);
        result.Reason.Should().Be(DepositSelectionService.FallbackReason);
    }

    [Fact]
    public void Zero_tier_amount_is_honoured_not_treated_as_missing()
    {
        // A configured 0 deposit is a real value (free deposit), not a fallback.
        var result = DepositSelectionService.Select(
            TenantVerificationTier.PartnerGuaranteed, Max, Unverified, Background, partnerGuaranteedCents: 0);

        result.AmountCents.Should().Be(0);
        result.Reason.Should().Be(DepositSelectionService.PartnerReason);
    }
}
