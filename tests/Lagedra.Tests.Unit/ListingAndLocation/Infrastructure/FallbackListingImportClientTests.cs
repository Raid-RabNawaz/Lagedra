using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Infrastructure;

public sealed class FallbackListingImportClientTests
{
    private static readonly Uri AirbnbUrl = new("https://www.airbnb.com/rooms/28198214");
    private static readonly Uri OtherUrl = new("https://rentals.example/listing/42");

    private static readonly string UsableAirbnbHtml =
        "<html><head><title>Lakeside</title>" +
        "<script id=\"data-deferred-state-0\" type=\"application/json\">{\"niobeClientData\":[]}</script>" +
        "</head><body>" + new string('x', 600) + "</body></html>";

    private static readonly string AirbnbShellHtml =
        "<html><head><title>Airbnb: Vacation Rentals</title></head><body>" + new string('x', 600) + "</body></html>";

    private static readonly string BotChallengeHtml =
        "<html><head><title>Just a moment...</title></head><body>" +
        "<div id=\"cf-challenge-running\"></div>" + new string('x', 600) + "</body></html>";

    private static readonly string UsableGenericHtml =
        "<html><head><title>Sunny Loft</title><meta property=\"og:title\" content=\"Sunny Loft\" /></head><body>" +
        new string('x', 600) + "</body></html>";

    private readonly IListingImportClient _primary = Substitute.For<IListingImportClient>();
    private readonly IListingImportClient _secondary = Substitute.For<IListingImportClient>();

    private FallbackListingImportClient CreateClient() =>
        new(_primary, _secondary, NullLogger<FallbackListingImportClient>.Instance);

    [Fact]
    public async Task FetchAsync_PrimaryUsable_NeverCallsSecondary()
    {
        var direct = new ListingFetchResult(UsableAirbnbHtml, AirbnbUrl, "text/html");
        _primary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns(direct);

        var result = await CreateClient().FetchAsync(AirbnbUrl);

        result.Should().BeSameAs(direct);
        await _secondary.DidNotReceiveWithAnyArgs().FetchAsync(default!, default);
    }

    [Fact]
    public async Task FetchAsync_PrimaryReturnsNull_UsesSecondary()
    {
        _primary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns((ListingFetchResult?)null);
        var recovered = new ListingFetchResult(UsableAirbnbHtml, AirbnbUrl, "text/html");
        _secondary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns(recovered);

        var result = await CreateClient().FetchAsync(AirbnbUrl);

        result.Should().BeSameAs(recovered);
    }

    [Fact]
    public async Task FetchAsync_PrimaryReturnsAirbnbShell_UsesSecondary()
    {
        // 200 OK but no server-rendered state: the import would be an empty draft.
        _primary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>())
            .Returns(new ListingFetchResult(AirbnbShellHtml, AirbnbUrl, "text/html"));
        var recovered = new ListingFetchResult(UsableAirbnbHtml, AirbnbUrl, "text/html");
        _secondary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns(recovered);

        var result = await CreateClient().FetchAsync(AirbnbUrl);

        result.Should().BeSameAs(recovered);
    }

    [Fact]
    public async Task FetchAsync_PrimaryReturnsBotChallenge_UsesSecondary()
    {
        _primary.FetchAsync(OtherUrl, Arg.Any<CancellationToken>())
            .Returns(new ListingFetchResult(BotChallengeHtml, OtherUrl, "text/html"));
        var recovered = new ListingFetchResult(UsableGenericHtml, OtherUrl, "text/html");
        _secondary.FetchAsync(OtherUrl, Arg.Any<CancellationToken>()).Returns(recovered);

        var result = await CreateClient().FetchAsync(OtherUrl);

        result.Should().BeSameAs(recovered);
    }

    [Fact]
    public async Task FetchAsync_NonAirbnbPlainPage_IsUsableWithoutSecondary()
    {
        // Generic hosts have no state blob to check for; a plain page is fine.
        var direct = new ListingFetchResult(UsableGenericHtml, OtherUrl, "text/html");
        _primary.FetchAsync(OtherUrl, Arg.Any<CancellationToken>()).Returns(direct);

        var result = await CreateClient().FetchAsync(OtherUrl);

        result.Should().BeSameAs(direct);
        await _secondary.DidNotReceiveWithAnyArgs().FetchAsync(default!, default);
    }

    [Fact]
    public async Task FetchAsync_BothThin_ReturnsSecondaryHtmlOverPrimary()
    {
        var direct = new ListingFetchResult(AirbnbShellHtml, AirbnbUrl, "text/html");
        var alsoThin = new ListingFetchResult(AirbnbShellHtml + "<!-- via ant -->", AirbnbUrl, "text/html");
        _primary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns(direct);
        _secondary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns(alsoThin);

        var result = await CreateClient().FetchAsync(AirbnbUrl);

        result.Should().BeSameAs(alsoThin);
    }

    [Fact]
    public async Task FetchAsync_SecondaryFails_KeepsPrimaryHtml()
    {
        // Better a thin page the extractor can read <title>/OG from than nothing.
        var direct = new ListingFetchResult(AirbnbShellHtml, AirbnbUrl, "text/html");
        _primary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns(direct);
        _secondary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns((ListingFetchResult?)null);

        var result = await CreateClient().FetchAsync(AirbnbUrl);

        result.Should().BeSameAs(direct);
    }

    [Fact]
    public async Task FetchAsync_BothNull_ReturnsNull()
    {
        _primary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns((ListingFetchResult?)null);
        _secondary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns((ListingFetchResult?)null);

        var result = await CreateClient().FetchAsync(AirbnbUrl);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchAsync_ScrapingAntQuotaExceeded_DirectFetcherTakesOver()
    {
        // Production default: ScrapingAnt is primary. When the subscription lapses
        // or the credit limit is hit it answers 403/429, which the ScrapingAnt
        // client surfaces as null — the direct fetcher must then serve the import.
        _primary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns((ListingFetchResult?)null);
        var direct = new ListingFetchResult(UsableAirbnbHtml, AirbnbUrl, "text/html");
        _secondary.FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns(direct);

        var result = await CreateClient().FetchAsync(AirbnbUrl);

        result.Should().BeSameAs(direct);
        await _primary.Received(1).FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>());
        await _secondary.Received(1).FetchAsync(AirbnbUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FetchRobotsAsync_AlwaysUsesSecondary()
    {
        // The secondary is the credit-free direct client in the default mode.
        _secondary.FetchRobotsAsync(AirbnbUrl, Arg.Any<CancellationToken>()).Returns("User-agent: *\nDisallow:");

        var robots = await CreateClient().FetchRobotsAsync(AirbnbUrl);

        robots.Should().StartWith("User-agent: *");
        await _primary.DidNotReceiveWithAnyArgs().FetchRobotsAsync(default!, default);
    }

    [Theory]
    [InlineData("Pardon Our Interruption")]
    [InlineData("Please verify you are a human")]
    [InlineData("px-captcha")]
    [InlineData("_Incapsula_Resource")]
    [InlineData("<h1>Access Denied</h1>")]
    public void Assess_KnownChallengeMarkers_AreBotChallenge(string marker)
    {
        var html = "<html><body>" + marker + new string('x', 600) + "</body></html>";

        ListingPageQuality.Assess(new ListingFetchResult(html, OtherUrl, "text/html"), OtherUrl)
            .Should().Be(ListingPageQuality.Verdict.BotChallenge);
    }

    [Fact]
    public void Assess_TinyBody_IsEmpty()
    {
        ListingPageQuality.Assess(new ListingFetchResult("<html></html>", OtherUrl, "text/html"), OtherUrl)
            .Should().Be(ListingPageQuality.Verdict.Empty);
    }

    [Fact]
    public void Assess_AirbnbWithOnlyJsonLd_IsUsable()
    {
        // Older/partial Airbnb responses may carry JSON-LD without the state blob;
        // the generic extractor can still read name/description/photos from it.
        var html = "<html><head><script type=\"application/ld+json\">{}</script></head><body>" +
            new string('x', 600) + "</body></html>";

        ListingPageQuality.Assess(new ListingFetchResult(html, AirbnbUrl, "text/html"), AirbnbUrl)
            .Should().Be(ListingPageQuality.Verdict.Usable);
    }
}
