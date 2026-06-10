using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lagedra.Tests.Unit.ListingAndLocation.Application;

public sealed class ImportListingFromUrlCommandTests
{
    private readonly IListingImportClient _client = Substitute.For<IListingImportClient>();
    private readonly IListingMetadataExtractor _extractor = new OpenGraphJsonLdExtractor();
    private readonly ILogger<ImportListingFromUrlCommandHandler> _logger =
        Substitute.For<ILogger<ImportListingFromUrlCommandHandler>>();

    private ImportListingFromUrlCommandHandler CreateHandler() => new(_client, _extractor, _logger);

    [Fact]
    public async Task Handle_MissingAttestation_FailsAndDoesNotFetch()
    {
        var handler = CreateHandler();
        var command = new ImportListingFromUrlCommand(
            Guid.NewGuid(),
            "https://example-rentals.test/listings/sunny-loft",
            HostAttestation: false);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Import.AttestationRequired");
        await _client.DidNotReceiveWithAnyArgs().FetchAsync(default!, default);
        await _client.DidNotReceiveWithAnyArgs().FetchRobotsAsync(default!, default);
    }

    [Fact]
    public async Task Handle_InvalidUrl_Fails()
    {
        var handler = CreateHandler();
        var command = new ImportListingFromUrlCommand(
            Guid.NewGuid(),
            "not-a-valid-url",
            HostAttestation: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Import.InvalidUrl");
        await _client.DidNotReceiveWithAnyArgs().FetchAsync(default!, default);
    }

    [Theory]
    [InlineData("ftp://example-rentals.test/listings")]
    [InlineData("/relative/path")]
    [InlineData("")]
    public async Task Handle_NonHttpOrRelativeUrl_FailsWithInvalidUrl(string url)
    {
        var handler = CreateHandler();
        var command = new ImportListingFromUrlCommand(Guid.NewGuid(), url, HostAttestation: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Import.InvalidUrl");
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsDraftFromFixtureHtml()
    {
        var url = new Uri("https://example-rentals.test/listings/sunny-loft");
        var html = ListingImportFixtures.Load("og-jsonld-apartment.html");

        _client.FetchRobotsAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _client.FetchAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ListingFetchResult?>(new ListingFetchResult(html, url, "text/html")));

        var handler = CreateHandler();
        var command = new ImportListingFromUrlCommand(Guid.NewGuid(), url.ToString(), HostAttestation: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Sunny Downtown Loft");
        result.Value.Bedrooms.Should().Be(2);
        result.Value.NightlyRateCents.Should().Be(18000);
        result.Value.SourceHost.Should().Be("example-rentals.test");
        result.Value.Photos.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_RobotsDisallowsPath_FailsAndDoesNotFetchPage()
    {
        var url = new Uri("https://example-rentals.test/listings/sunny-loft");

        _client.FetchRobotsAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("User-agent: *\nDisallow: /listings"));

        var handler = CreateHandler();
        var command = new ImportListingFromUrlCommand(Guid.NewGuid(), url.ToString(), HostAttestation: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Import.RobotsBlocked");
        await _client.DidNotReceiveWithAnyArgs().FetchAsync(default!, default);
    }

    [Fact]
    public async Task Handle_FetchReturnsNull_FailsWithFetchFailed()
    {
        var url = new Uri("https://example-rentals.test/listings/sunny-loft");

        _client.FetchRobotsAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));
        _client.FetchAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<ListingFetchResult?>(null));

        var handler = CreateHandler();
        var command = new ImportListingFromUrlCommand(Guid.NewGuid(), url.ToString(), HostAttestation: true);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Import.FetchFailed");
    }
}
