using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Infrastructure.External.Channels.Hosthub;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

/// <summary>
/// Pins Hosthub public-API paths, ApiKeyAuth header fallback, and response
/// mapping against canned payloads from the 2019-03-01 docs.
/// </summary>
public sealed class HosthubChannelProviderTests
{
    private const string ApiKey = "hh_test_key_abc123";

    private static readonly ChannelCredentials Credentials = new(
        ProviderKey: "hosthub",
        ExternalAccountId: "••••c123",
        Secret: ApiKey);

    private static HosthubChannelProvider CreateProvider(
        StubHandler handler,
        HosthubChannelSettings? settings = null)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("https://app.hosthub.com") },
            Options.Create(settings ?? new HosthubChannelSettings()),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<HosthubChannelProvider>.Instance);

    // ── Listing import ───────────────────────────────────────────────────────

    [Fact]
    public async Task PullListingsAsync_MapsRentalListAndEnrichesRate()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/2019-03-01/rentals" => Ok(RentalsList),
            "/api/2019-03-01/rentals/w54dWRE" => Ok(RentalDetail),
            "/api/2019-03-01/rentals/w54dWRE/rate-plans" => Ok(RatePlans),
            "/api/2019-03-01/rate-plans/xEadpJrLqo/rates" => Ok(DailyRates),
            _ => (HttpStatusCode.NotFound, "{}"),
        });

        var snapshots = await CreateProvider(handler)
            .PullListingsAsync(Credentials, CancellationToken.None);

        snapshots.Should().HaveCount(1);
        var snapshot = snapshots[0];
        snapshot.ExternalListingId.Should().Be("w54dWRE");
        snapshot.Title.Should().Be("The home of Tasos, Jo and Luis");
        snapshot.Currency.Should().Be("USD");
        snapshot.NightlyRateCents.Should().Be(12_245);
        snapshot.MonthlyRentCents.Should().Be(367_350);
        snapshot.Latitude.Should().BeApproximately(12.458745, 0.000001);
        snapshot.Longitude.Should().BeApproximately(31.684515, 0.000001);
        snapshot.Address.Should().NotBeNull();
        snapshot.Address!.City.Should().Be("London");
        snapshot.Address.Country.Should().Be("GB");
        snapshot.Address.PostalCode.Should().Be("14231");
        snapshot.Photos.Should().ContainSingle(p => p.Url.ToString().Contains("cover.jpg", StringComparison.Ordinal));
        snapshot.MinStayNights.Should().Be(2);
        snapshot.MaxStayNights.Should().Be(4);
    }

    [Fact]
    public async Task PullListingsAsync_WithoutSecret_MakesNoRequests()
    {
        var handler = new StubHandler(_ => Ok(RentalsList));
        var credentials = new ChannelCredentials("hosthub", "hosthub");

        var snapshots = await CreateProvider(handler)
            .PullListingsAsync(credentials, CancellationToken.None);

        snapshots.Should().BeEmpty();
        handler.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Requests_SendRawAuthorizationKey()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/2019-03-01/rentals" => Ok("""{"object":"Rental","data":[]}"""),
            _ => (HttpStatusCode.NotFound, "{}"),
        });

        await CreateProvider(handler).PullListingsAsync(Credentials, CancellationToken.None);

        handler.Calls[0].Headers.Should().ContainKey("Authorization")
            .WhoseValue.Should().Be(ApiKey);
    }

    [Fact]
    public async Task Requests_RetryBearerAfterUnauthorized()
    {
        var attempts = 0;
        var handler = new StubHandler(request =>
        {
            attempts++;
            if (attempts == 1)
            {
                return (HttpStatusCode.Unauthorized, """{"error":"unauthorized"}""");
            }

            return request.RequestUri!.AbsolutePath == "/api/2019-03-01/rentals"
                ? Ok("""{"object":"Rental","data":[]}""")
                : (HttpStatusCode.NotFound, "{}");
        });

        await CreateProvider(handler).PullListingsAsync(Credentials, CancellationToken.None);

        handler.Calls.Should().HaveCountGreaterThanOrEqualTo(2);
        handler.Calls[0].Headers["Authorization"].Should().Be(ApiKey);
        handler.Calls[1].Headers["Authorization"].Should().Be($"Bearer {ApiKey}");
    }

    [Fact]
    public async Task PullListingsAsync_FollowsNavigationCursor()
    {
        var handler = new StubHandler(request =>
        {
            var query = request.RequestUri!.Query;
            if (query.Contains("cursor_gt=", StringComparison.Ordinal))
            {
                return Ok("""
                    {"object":"Rental","data":[
                      {"id":"second","object":"Rental","name":"Second","currency":"USD"}
                    ]}
                    """);
            }

            return Ok("""
                {"object":"Rental","data":[
                  {"id":"first","object":"Rental","name":"First","currency":"USD"}
                ],"navigation":{"next":"/api/2019-03-01/rentals?cursor_gt=abc"}}
                """);
        });

        var snapshots = await CreateProvider(handler)
            .PullListingsAsync(Credentials, CancellationToken.None);

        snapshots.Select(s => s.ExternalListingId).Should().Equal("first", "second");
    }

    // ── Availability ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PullAvailabilityAsync_MarksEventNightsUnavailable()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var handler = new StubHandler(_ => Ok($$"""
            {"object":"CalendarEvent","data":[{
              "id":"er23321",
              "object":"CalendarEvent",
              "type":"CalendarEventBooking",
              "is_visible":true,
              "date_from":"{{Iso(today)}}",
              "date_to":"{{Iso(today.AddDays(2))}}"
            }]}
            """));

        var calendar = await CreateProvider(handler)
            .PullAvailabilityAsync(Credentials, "w54dWRE", CancellationToken.None);

        IsAvailable(calendar, today).Should().BeFalse();
        IsAvailable(calendar, today.AddDays(1)).Should().BeFalse();
        IsAvailable(calendar, today.AddDays(2)).Should().BeTrue();
        handler.Calls[0].Path.Should().Be("/api/2019-03-01/rentals/w54dWRE/calendar-events");
    }

    [Fact]
    public async Task CheckAvailabilityAsync_Overlap_ReturnsUnavailable()
    {
        var handler = new StubHandler(_ => Ok("""
            {"object":"CalendarEvent","data":[{
              "id":"er23321","type":"CalendarEventBooking","is_visible":true,
              "date_from":"2026-09-01","date_to":"2026-09-04"
            }]}
            """));
        var query = new ChannelAvailabilityQuery(
            "w54dWRE", new DateOnly(2026, 9, 2), new DateOnly(2026, 9, 5));

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeFalse();
        result.ErrorCode.Should().Be("Unavailable");
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ClearCalendar_ReturnsAvailable()
    {
        var handler = new StubHandler(_ => Ok("""{"object":"CalendarEvent","data":[]}"""));
        var query = new ChannelAvailabilityQuery(
            "w54dWRE", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5));

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAvailabilityAsync_InvertedDates_ShortCircuits()
    {
        var handler = new StubHandler(_ => Ok("{}"));
        var query = new ChannelAvailabilityQuery(
            "w54dWRE", new DateOnly(2026, 9, 5), new DateOnly(2026, 9, 1));

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidDates");
        handler.Calls.Should().BeEmpty();
    }

    // ── Booking push ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PushBookingAsync_SendsPaidBookingAndReturnsId()
    {
        var handler = new StubHandler(request =>
            (request.Method.Method, request.RequestUri!.AbsolutePath) switch
            {
                ("POST", "/api/2019-03-01/rentals/w54dWRE/calendar-events") =>
                    Ok("""{"id":"er23321","object":"CalendarEvent","type":"CalendarEventBooking"}""", HttpStatusCode.Created),
                _ => (HttpStatusCode.NotFound, "{}"),
            });

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, SampleBooking(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExternalBookingId.Should().Be("er23321");

        var body = handler.Calls.Single().Body!;
        body.Should().Contain("\"type\":\"Booking\"");
        body.Should().Contain("\"date_from\":\"2026-09-01\"");
        body.Should().Contain("\"date_to\":\"2026-09-08\"");
        body.Should().Contain("\"guest_name\":\"Ada Lovelace\"");
        body.Should().Contain("\"guest_email\":\"ada@example.com\"");
        body.Should().Contain("\"reservation_id\":\"LGD-TRACK-1\"");
        body.Should().Contain("\"cents\":140000");
        body.Should().Contain("\"currency\":\"USD\"");
        body.Should().Contain("\"guest_adults\":2");
        body.Should().NotContain("source_id");
    }

    [Fact]
    public async Task PushBookingAsync_IncludesConfiguredSourceId()
    {
        var handler = new StubHandler(_ =>
            Ok("""{"id":"er23321"}""", HttpStatusCode.Created));
        var settings = new HosthubChannelSettings { SourceId = "As7a1G" };

        await CreateProvider(handler, settings)
            .PushBookingAsync(Credentials, SampleBooking(), CancellationToken.None);

        handler.Calls.Single().Body.Should().Contain("\"source_id\":\"As7a1G\"");
    }

    [Fact]
    public async Task PushBookingAsync_RejectedByHosthub_Fails()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.BadRequest,
            """{"message":"date_from is invalid"}"""));

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, SampleBooking(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("RequestFailed");
        result.ErrorMessage.Should().Contain("date_from");
    }

    [Fact]
    public async Task PushBookingAsync_MissingListingId_FailsWithoutCallingApi()
    {
        var handler = new StubHandler(_ => Ok("{}"));
        var request = SampleBooking() with { ExternalListingId = "" };

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, request, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidListingId");
        handler.Calls.Should().BeEmpty();
    }

    // ── Booking updates ──────────────────────────────────────────────────────

    [Fact]
    public async Task PullBookingUpdatesAsync_MapsVisibleAndCancelled_SkipsHolds()
    {
        var handler = new StubHandler(_ => Ok("""
            {"object":"CalendarEvent","data":[
              {"id":"b1","type":"CalendarEventBooking","is_visible":true,"updated":1721400000},
              {"id":"b2","type":"CalendarEventBooking","is_visible":false,"cancelled_at":1721486400,"updated":1721486400},
              {"id":"h1","type":"CalendarEventHold","is_visible":true,"updated":1721572800}
            ]}
            """));

        var updates = await CreateProvider(handler).PullBookingUpdatesAsync(
            Credentials, new DateTime(2024, 7, 18, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        updates.Should().HaveCount(2);
        updates.Select(u => (u.ExternalBookingId, u.Status)).Should().Equal(
            ("b1", "confirmed"),
            ("b2", "cancelled"));
        handler.Calls[0].Path.Should().Be("/api/2019-03-01/calendar-events");
        handler.Calls[0].Query.Should().Contain("updated_gt=");
        handler.Calls[0].Query.Should().Contain("is_visible=all");
    }

    [Fact]
    public async Task PullBookingUpdatesAsync_ApiFailure_ReturnsEmpty()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Unauthorized, """{"error":"no"}"""));

        var updates = await CreateProvider(handler).PullBookingUpdatesAsync(
            Credentials, DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        updates.Should().BeEmpty();
    }

    // ── Fixtures / helpers ───────────────────────────────────────────────────

    private const string RentalsList = """
        {"object":"Rental","data":[{
          "id":"w54dWRE",
          "object":"Rental",
          "name":"The home of Tasos, Jo and Luis",
          "city":"London",
          "country":"GB",
          "postal_code":"14231",
          "latitude":"12.458745",
          "longitude":"31.684515",
          "image_path":"https://cdn.hosthub.com/cover.jpg",
          "currency":"GBP"
        }]}
        """;

    private const string RentalDetail = """
        {"id":"w54dWRE","object":"Rental","name":"The home of Tasos, Jo and Luis",
         "city":"London","country":"GB","postal_code":"14231",
         "latitude":"12.458745","longitude":"31.684515",
         "image_path":"https://cdn.hosthub.com/cover.jpg","currency":"GBP"}
        """;

    private const string RatePlans = """
        {"object":"RatePlan","data":[
          {"id":"xEadpJrLqo","name":"Default rate plan","default":true,"status":"active"}
        ]}
        """;

    private const string DailyRates = """
        {"object":"RentalDailyRate","data":[
          {"object":"RentalDailyRate","date":"2019-03-01",
           "amount":{"cents":12245,"currency":"USD"},
           "minimum_length_of_stay":2,"maximum_length_of_stay":4}
        ]}
        """;

    private static ChannelBookingPushRequest SampleBooking() => new(
        ExternalListingId: "w54dWRE",
        Guest: new ChannelGuest("Ada", "Lovelace", "ada@example.com", "+15551234567"),
        CheckIn: new DateOnly(2026, 9, 1),
        CheckOut: new DateOnly(2026, 9, 8),
        Adults: 2,
        Children: 1,
        Pets: 0,
        Currency: "USD",
        OrderItems:
        [
            new ChannelOrderItem("rent", "Nightly rent", 120_000),
            new ChannelOrderItem("cleaning", "Cleaning fee", 20_000),
        ],
        PaymentStatus: "paid",
        TrackingReference: "LGD-TRACK-1");

    private static (HttpStatusCode, string) Ok(string body, HttpStatusCode status = HttpStatusCode.OK)
        => (status, body);

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static bool IsAvailable(ChannelAvailabilityCalendar calendar, DateOnly date)
        => calendar.Blocks.Single(b => date >= b.Start && date <= b.End).Available;

    private sealed record Call(
        string Method,
        string Path,
        string Query,
        Dictionary<string, string> Headers,
        string? Body);

    private sealed class StubHandler(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> router)
        : HttpMessageHandler
    {
        public List<Call> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            Calls.Add(new Call(
                request.Method.Method,
                request.RequestUri!.AbsolutePath,
                request.RequestUri.Query,
                request.Headers.ToDictionary(
                    h => h.Key,
                    h => string.Join(",", h.Value),
                    StringComparer.OrdinalIgnoreCase),
                body));

            var (status, responseBody) = router(request);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };
        }
    }
}
