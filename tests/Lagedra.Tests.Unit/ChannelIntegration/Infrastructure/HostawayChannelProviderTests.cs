using System.Net;
using System.Text;
using FluentAssertions;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Infrastructure.External.Channels.Hostaway;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

/// <summary>
/// Pins Hostaway listing price mapping: a normal base price stays nightly × 30,
/// and an unlabeled base price that would become an absurd monthly rent is kept
/// as the monthly amount (the Reno $8,607 case).
/// </summary>
public sealed class HostawayChannelProviderTests
{
    private static readonly ChannelCredentials Credentials = new(
        ProviderKey: "hostaway",
        ExternalAccountId: "40160",
        Secret: "hostaway-client-secret");

    [Fact]
    public async Task PullListingsAsync_DistinguishesNightlyBasePriceFromMonthlySizedAmount()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/v1/accessTokens" => Ok("""{"token_type":"Bearer","expires_in":3600,"access_token":"tok_test"}"""),
            "/v1/listings" => Ok(ListingsPage),
            _ => (HttpStatusCode.NotFound, "{}"),
        });

        var snapshots = await CreateProvider(handler)
            .PullListingsAsync(Credentials, CancellationToken.None);

        snapshots.Should().HaveCount(2);

        var nightly = snapshots.Single(s => s.ExternalListingId == "40160");
        nightly.Title.Should().Be("Cozy apartment");
        nightly.PriceBasis.Should().Be(ChannelPriceBasis.Nightly);
        nightly.NightlyRateCents.Should().Be(21_162); // $211.62
        nightly.MonthlyRentCents.Should().Be(634_860);
        nightly.Currency.Should().Be("USD");

        var monthly = snapshots.Single(s => s.ExternalListingId == "8607");
        monthly.Title.Should().Be("Reno");
        monthly.PriceBasis.Should().Be(ChannelPriceBasis.Monthly);
        monthly.MonthlyRentCents.Should().Be(860_700); // $8,607, not × 30
        monthly.NightlyRateCents.Should().BeNull();
    }

    private static HostawayChannelProvider CreateProvider(StubHandler handler)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.hostaway.com") },
            Options.Create(new HostawayChannelSettings()),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<HostawayChannelProvider>.Instance);

    private const string ListingsPage = """
        {
          "status": "success",
          "result": [
            {
              "id": 40160,
              "name": "Cozy apartment",
              "price": 211.62,
              "currencyCode": "USD",
              "monthlyDiscount": 0.82,
              "minNights": 2
            },
            {
              "id": 8607,
              "name": "Reno",
              "price": 8607,
              "currencyCode": "USD",
              "minNights": 30
            }
          ]
        }
        """;

    private static (HttpStatusCode, string) Ok(string body) => (HttpStatusCode.OK, body);

    private sealed class StubHandler(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> router)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var (status, responseBody) = router(request);
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            });
        }
    }
}
