using System.Collections.Generic;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.ScrapingAnt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Infrastructure;

/// <summary>
/// Guards the DI wiring for the "import from URL" fetcher so a config change
/// (adding/removing the ScrapingAnt key, toggling FallbackOnly) cannot leave
/// <see cref="IListingImportClient"/> unresolvable or routed to the wrong client.
/// </summary>
public sealed class ListingImportFetcherRegistrationTests
{
    [Fact]
    public void NoScrapingAntKey_ResolvesDirectHttpClient()
    {
        var client = Resolve(new Dictionary<string, string?>());

        client.Should().BeOfType<HttpListingImportClient>();
    }

    [Fact]
    public void ScrapingAntKey_DefaultMode_ScrapingAntFirstThenDirect()
    {
        var client = Resolve(new Dictionary<string, string?>
        {
            ["ListingImport:ScrapingAnt:ApiKey"] = "test-key",
        });

        var composite = client.Should().BeOfType<FallbackListingImportClient>().Subject;
        composite.Primary.Should().BeOfType<ScrapingAntListingImportClient>();
        composite.Secondary.Should().BeOfType<HttpListingImportClient>();
    }

    [Fact]
    public void ScrapingAntKey_FallbackMode_DirectFirstThenScrapingAnt()
    {
        var client = Resolve(new Dictionary<string, string?>
        {
            ["ListingImport:ScrapingAnt:ApiKey"] = "test-key",
            ["ListingImport:ScrapingAnt:Mode"] = "Fallback",
        });

        var composite = client.Should().BeOfType<FallbackListingImportClient>().Subject;
        composite.Primary.Should().BeOfType<HttpListingImportClient>();
        composite.Secondary.Should().BeOfType<ScrapingAntListingImportClient>();
    }

    [Fact]
    public void ScrapingAntMode_DefaultsToPrimary()
    {
        new ScrapingAntSettings().Mode.Should().Be(ScrapingAntMode.Primary);
    }

    [Fact]
    public void ScrapingAntMode_BindsFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ListingImport:ScrapingAnt:Mode"] = "Fallback",
            })
            .Build();

        configuration.GetSection(ScrapingAntSettings.SectionName).Get<ScrapingAntSettings>()!
            .Mode.Should().Be(ScrapingAntMode.Fallback);
    }

    [Fact]
    public void BlankScrapingAntKey_IsTreatedAsAbsent()
    {
        var client = Resolve(new Dictionary<string, string?>
        {
            ["ListingImport:ScrapingAnt:ApiKey"] = "   ",
        });

        client.Should().BeOfType<HttpListingImportClient>();
    }

    private static IListingImportClient Resolve(Dictionary<string, string?> values)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.None));
        services.AddListingImportFetcher(configuration);

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        return provider.GetRequiredService<IListingImportClient>();
    }
}
