using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Truvi;
using Lagedra.SharedKernel.Insurance;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lagedra.Tests.Unit.InsuranceIntegration.Infrastructure;

public class TruviScreenAndProtectClientTests
{
    private static readonly Guid DealId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task Create_sends_subscription_key_and_maps_approved()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK,
            """{"verificationId":"ver_1","status":"Approved"}"""));
        var client = CreateClient(handler);

        var result = await client.CreateAsync(SampleCreate(), CancellationToken.None);

        result.VerificationId.Should().Be("ver_1");
        result.Status.Should().Be(TruviScreeningStatus.Approved);
        handler.LastRequest!.Headers.Contains("Ocp-Apim-Subscription-Key").Should().BeTrue();
        handler.LastRequest.RequestUri!.AbsolutePath.Should().EndWith("/verificationRequests");
        handler.LastRequest.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task Create_maps_flagged_and_rejected()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK,
            """{"verificationId":"ver_2","status":"Flagged","flaggedReason":"Watchlist"}"""));
        var flagged = await CreateClient(handler).CreateAsync(SampleCreate(), CancellationToken.None);
        flagged.Status.Should().Be(TruviScreeningStatus.Flagged);
        flagged.FlaggedReason.Should().Be("Watchlist");

        handler.Router = _ => (HttpStatusCode.OK, """{"verificationId":"ver_3","status":"Rejected"}""");
        var rejected = await CreateClient(handler).CreateAsync(SampleCreate(), CancellationToken.None);
        rejected.Status.Should().Be(TruviScreeningStatus.Rejected);
    }

    [Fact]
    public async Task Create_throws_rfc7807_detail()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.BadRequest,
            """{"title":"Validation error","detail":"checkIn cannot be in the past","status":400}"""));
        var client = CreateClient(handler);

        var act = () => client.CreateAsync(SampleCreate(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<TruviScreenAndProtectException>();
        ex.Which.Detail.Should().Contain("checkIn cannot be in the past");
        ex.Which.Status.Should().Be(400);
    }

    [Fact]
    public async Task Cancel_puts_cancel_path()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, """{"timeStamp":"x","echoToken":"y"}"""));
        var client = CreateClient(handler);

        await client.CancelAsync(
            TruviVerificationRequestFactory.Cancel(DealId, DateTime.UtcNow, "ver_1"),
            CancellationToken.None);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.AbsolutePath.Should().EndWith("/verificationRequests/cancel");
    }

    [Fact]
    public async Task Modify_puts_verification_requests()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.OK, """{"timeStamp":"x","echoToken":"y"}"""));
        var client = CreateClient(handler);

        await client.ModifyAsync(
            TruviVerificationRequestFactory.Modify(
                DealId,
                DateTime.UtcNow,
                "ver_1",
                DealId.ToString("D"),
                new DateOnly(2026, 10, 15),
                new DateOnly(2026, 12, 14),
                petsAllowed: false),
            CancellationToken.None);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.AbsolutePath.Should().EndWith("/verificationRequests");
        handler.LastRequest.RequestUri.AbsolutePath.Should().NotEndWith("/cancel");
    }

    private static TruviScreenAndProtectClient CreateClient(StubHandler handler)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("https://developer.api.truvi.com/screen-and-protect-sandbox/") },
            Options.Create(new TruviScreenAndProtectSettings { SubscriptionKey = "test-key" }),
            NullLogger<TruviScreenAndProtectClient>.Instance);

    private static TruviCreateVerificationRequest SampleCreate()
    {
        TruviVerificationRequestFactory.TryCreate(
            DealId,
            new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc),
            "Lagedra",
            "raid@lagedra.com",
            50_000,
            "12 Main St",
            "Los Angeles",
            "90012",
            "USA",
            false,
            1,
            1,
            1m,
            new DateOnly(2026, 10, 15),
            new DateOnly(2026, 11, 14),
            "Ada",
            "Lovelace",
            "Ada Lovelace",
            "ada@example.com",
            null,
            out var request,
            out _).Should().BeTrue();
        return request!;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> Router { get; set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        public StubHandler(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> router)
        {
            Router = router;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content is not null)
            {
                await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }

            var (status, body) = Router(request);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        }
    }
}
