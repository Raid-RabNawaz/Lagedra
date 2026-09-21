using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.SharedKernel.Insurance;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Settings;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.ActivationAndBilling.Application;

public class ReservationPricingServiceTests
{
    private static readonly Guid ListingId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private static readonly Guid LandlordId = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly Guid TenantId = Guid.Parse("66666666-7777-8888-9999-000000000000");

    [Fact]
    public async Task Includes_insurance_when_listing_has_stay_protection()
    {
        var calculator = Substitute.For<IInsuranceFeeCalculator>();
        calculator.CalculateFeeAsync(275_000, 30, Arg.Any<CancellationToken>())
            .Returns(new InsuranceFeeQuote(18_000, "Truvi", null));

        var sut = CreateSut(calculator);
        var pricing = await sut.ComputeAsync(Listing(stayProtectionEnabled: true), TenantId, 30);

        pricing.InsuranceFeeCents.Should().Be(18_000);
        pricing.TotalPayableCents.Should().Be(275_000 + 500_000 + 18_000);
        await calculator.Received(1).CalculateFeeAsync(275_000, 30, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Omits_insurance_when_host_opts_out()
    {
        var calculator = Substitute.For<IInsuranceFeeCalculator>();
        calculator.CalculateFeeAsync(Arg.Any<long>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new InsuranceFeeQuote(18_000, "Truvi", null));

        var sut = CreateSut(calculator);
        var pricing = await sut.ComputeAsync(Listing(stayProtectionEnabled: false), TenantId, 30);

        pricing.InsuranceFeeCents.Should().Be(0);
        pricing.TotalPayableCents.Should().Be(275_000 + 500_000);
        await calculator.DidNotReceiveWithAnyArgs()
            .CalculateFeeAsync(default, default, default);
    }

    private static ReservationPricingService CreateSut(IInsuranceFeeCalculator calculator)
    {
        var tiers = Substitute.For<ITenantVerificationTierResolver>();
        tiers.ResolveAsync(TenantId, Arg.Any<CancellationToken>())
            .Returns(new TenantVerificationTierResult(TenantVerificationTier.Unverified, null));

        var settings = Substitute.For<IPlatformSettingsService>();
        settings.GetBoolAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.ArgAt<bool>(1)));
        settings.GetLongAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(call => Task.FromResult(call.ArgAt<long>(1)));

        var offers = Substitute.For<IAcceptedInquiryOfferProvider>();
        offers.GetAcceptedOfferAsync(ListingId, TenantId, Arg.Any<CancellationToken>())
            .Returns((AcceptedInquiryOfferDto?)null);

        return new ReservationPricingService(tiers, calculator, settings, offers);
    }

    private static ListingDetailsDto Listing(bool stayProtectionEnabled) =>
        new(
            ListingId,
            LandlordId,
            30,
            180,
            500_000,
            275_000,
            null,
            StayProtectionEnabled: stayProtectionEnabled);
}
