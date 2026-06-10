using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.ScrapingAnt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Infrastructure;

public sealed class ScrapingAntListingImportClientTests
{
    private static readonly Uri Target = new("https://www.airbnb.com/rooms/12345678");

    private static ScrapingAntListingImportClient CreateClient(
        StubHandler handler,
        ScrapingAntSettings? settings = null)
    {
        var antClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.scrapingant.com/v2/"),
        };
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(new HttpClient(new StubHandler(HttpStatusCode.NotFound, "")));

        return new ScrapingAntListingImportClient(
            antClient,
            factory,
            Options.Create(settings ?? new ScrapingAntSettings { ApiKey = "test-key" }),
            NullLogger<ScrapingAntListingImportClient>.Instance);
    }

    [Fact]
    public async Task FetchAsync_Success_ReturnsRenderedHtml()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "<html><head><title>Cosy Loft</title></head><body>ok</body></html>");
        var client = CreateClient(handler);

        var result = await client.FetchAsync(Target, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Html.Should().Contain("Cosy Loft");
        result.FinalUrl.Should().Be(Target);
    }

    [Fact]
    public async Task FetchAsync_BuildsExpectedScrapingAntQuery()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "<html></html>");
        var settings = new ScrapingAntSettings
        {
            ApiKey = "test-key",
            ProxyType = "residential",
            ProxyCountry = "us",
            Browser = true,
            BlockResources = "image,media,font",
            TimeoutSeconds = 60,
        };
        var client = CreateClient(handler, settings);

        await client.FetchAsync(Target, CancellationToken.None);

        var requested = handler.LastRequestUri!.ToString();
        requested.Should().Contain("/v2/general");
        requested.Should().Contain("url=" + Uri.EscapeDataString(Target.ToString()));
        requested.Should().Contain("browser=true");
        requested.Should().Contain("return_page_source=false");
        requested.Should().Contain("proxy_type=residential");
        requested.Should().Contain("proxy_country=us");
        requested.Should().Contain("block_resource=image");
        requested.Should().Contain("block_resource=media");
        requested.Should().Contain("block_resource=font");
        requested.Should().Contain("timeout=60");
    }

    [Fact]
    public async Task FetchAsync_ApiError_ReturnsNull()
    {
        var handler = new StubHandler(
            HttpStatusCode.Forbidden,
            "{\"detail\":\"The website can not be scraped, please retry\"}");
        var client = CreateClient(handler);

        var result = await client.FetchAsync(Target, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task FetchAsync_EmptyBody_ReturnsNull()
    {
        var handler = new StubHandler(HttpStatusCode.OK, "   ");
        var client = CreateClient(handler);

        var result = await client.FetchAsync(Target, CancellationToken.None);

        result.Should().BeNull();
    }

    private sealed class StubHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
    {
        public Uri? LastRequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body),
            });
        }
    }
}
