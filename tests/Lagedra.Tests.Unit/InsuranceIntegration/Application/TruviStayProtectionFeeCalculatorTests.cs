using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lagedra.Tests.Unit.InsuranceIntegration.Application;

public class TruviStayProtectionFeeCalculatorTests
{
    [Fact]
    public async Task Returns_zero_when_subscription_key_is_missing()
    {
        var sut = CreateSut(subscriptionKey: null);

        var quote = await sut.CalculateFeeAsync(275_000, 30, CancellationToken.None);

        quote.FeeCents.Should().Be(0);
        quote.Provider.Should().Be("None");
    }

    [Fact]
    public async Task Returns_zero_when_screening_is_disabled()
    {
        var sut = CreateSut(subscriptionKey: "test-key", screeningEnabled: false);

        var quote = await sut.CalculateFeeAsync(275_000, 30, CancellationToken.None);

        quote.FeeCents.Should().Be(0);
    }

    [Fact]
    public async Task Quotes_nightly_schedule_when_key_is_configured()
    {
        var sut = CreateSut(subscriptionKey: "test-key");

        var quote = await sut.CalculateFeeAsync(275_000, 30, CancellationToken.None);

        quote.FeeCents.Should().Be(18_000);
        quote.Provider.Should().Be("Truvi");
    }

    private static TruviStayProtectionFeeCalculator CreateSut(
        string? subscriptionKey,
        bool screeningEnabled = true)
        => new(
            new ConfigurationBuilder().Build(),
            Options.Create(new TruviScreenAndProtectSettings
            {
                ScreeningEnabled = screeningEnabled,
                SubscriptionKey = subscriptionKey,
            }));
}
