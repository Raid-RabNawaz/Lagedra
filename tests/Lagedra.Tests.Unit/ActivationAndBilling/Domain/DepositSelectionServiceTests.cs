using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Domain.Services;
using Lagedra.SharedKernel.Integration;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Domain;

public class DepositSelectionServiceTests
{
    private const long Max = 500_000;
    private const long Unverified = 300_000;
    private const long Trusted = 200_000;
    private const long Elite = 100_000;

    [Fact]
    public void Elite_tier_selects_partner_deposit_column()
    {
        var result = DepositSelectionService.Select(
            TenantVerificationTier.Elite, Max, Unverified, Trusted, Elite);

        result.AmountCents.Should().Be(Elite);
        result.Tier.Should().Be(TenantVerificationTier.Elite);
        result.Reason.Should().Be(DepositSelectionService.EliteReason);
    }

    [Fact]
    public void Trusted_tier_selects_background_verified_deposit_column()
    {
        var result = DepositSelectionService.Select(
            TenantVerificationTier.Trusted, Max, Unverified, Trusted, Elite);

        result.AmountCents.Should().Be(Trusted);
        result.Reason.Should().Be(DepositSelectionService.TrustedReason);
    }

    [Fact]
    public void Verified_uses_standard_deposit()
    {
        var result = DepositSelectionService.Select(
            TenantVerificationTier.Verified, Max, Unverified, Trusted, Elite);

        result.AmountCents.Should().Be(Unverified);
        result.Reason.Should().Be(DepositSelectionService.UnverifiedReason);
    }

    [Fact]
    public void Unverified_tier_selects_unverified_deposit_with_reason()
    {
        var result = DepositSelectionService.Select(
            TenantVerificationTier.Unverified, Max, Unverified, Trusted, Elite);

        result.AmountCents.Should().Be(Unverified);
        result.Reason.Should().Be(DepositSelectionService.UnverifiedReason);
    }

    [Theory]
    [InlineData(TenantVerificationTier.Elite)]
    [InlineData(TenantVerificationTier.Trusted)]
    [InlineData(TenantVerificationTier.Unverified)]
    [InlineData(TenantVerificationTier.Verified)]
    public void Missing_tier_amount_falls_back_to_max_deposit(TenantVerificationTier tier)
    {
        var result = DepositSelectionService.Select(tier, Max, null, null, null);

        result.AmountCents.Should().Be(Max);
        result.Tier.Should().Be(tier);
        result.Reason.Should().Be(DepositSelectionService.FallbackReason);
    }

    [Fact]
    public void Zero_tier_amount_is_honoured_not_treated_as_missing()
    {
        var result = DepositSelectionService.Select(
            TenantVerificationTier.Elite, Max, Unverified, Trusted, partnerGuaranteedCents: 0);

        result.AmountCents.Should().Be(0);
        result.Reason.Should().Be(DepositSelectionService.EliteReason);
    }
}
