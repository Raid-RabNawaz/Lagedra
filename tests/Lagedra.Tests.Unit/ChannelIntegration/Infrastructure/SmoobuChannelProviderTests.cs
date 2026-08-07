using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Infrastructure.External.Channels.Smoobu;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

/// <summary>
/// Exercises the Smoobu provider against canned payloads so the HMAC request
/// signing, endpoint shapes, and response mapping are all pinned.
/// </summary>
public sealed class SmoobuChannelProviderTests
{
    private const string ApiKey = "usr_live_abc123";
    private const string ApiSecret = "smoobu_api_secret";

    private static readonly ChannelCredentials Credentials = new(
        ProviderKey: "smoobu",
        ExternalAccountId: ApiKey,
        Secret: ApiSecret);

    private static SmoobuChannelProvider CreateProvider(
        StubHandler handler,
        SmoobuChannelSettings? settings = null)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("https://login.smoobu.com") },
            Options.Create(settings ?? new SmoobuChannelSettings()),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<SmoobuChannelProvider>.Instance);

    // ── Listing import ───────────────────────────────────────────────────────

    [Fact]
    public async Task PullListingsAsync_MergesIndexAndDetail()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/apartments" => Ok(ApartmentsIndex),
            "/api/apartments/1" => Ok(ApartmentDetail),
            _ => (HttpStatusCode.NotFound, "{}"),
        });

        var snapshots = await CreateProvider(handler)
            .PullListingsAsync(Credentials, CancellationToken.None);

        snapshots.Should().HaveCount(1);
        var snapshot = snapshots[0];

        snapshot.ExternalListingId.Should().Be("1");
        snapshot.Title.Should().Be("Seaside apartment");
        snapshot.Currency.Should().Be("EUR");
        // price.minimal "85.00" → nightly cents; monthly is nightly × 30.
        snapshot.NightlyRateCents.Should().Be(8_500);
        snapshot.MonthlyRentCents.Should().Be(255_000);
        snapshot.Bedrooms.Should().Be(4);
        snapshot.Bathrooms.Should().Be(2m);
        snapshot.Latitude.Should().BeApproximately(52.5200, 0.0001);
        snapshot.Longitude.Should().BeApproximately(13.4050, 0.0001);
        snapshot.PropertyType.Should().Be("other"); // "Holiday rental"

        snapshot.Address.Should().NotBeNull();
        snapshot.Address!.Line1.Should().Be("Wönnichstr. 68/70");
        snapshot.Address.City.Should().Be("Berlin");
        snapshot.Address.PostalCode.Should().Be("10317");
        snapshot.Address.Country.Should().Be("Germany");

        snapshot.AmenityCodes.Should().BeEquivalentTo(
            new[] { "Internet", "Pool", "Heating" },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task PullListingsAsync_DetailFailure_StillEmitsMinimalSnapshot()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/apartments" => Ok(ApartmentsIndex),
            _ => (HttpStatusCode.InternalServerError, "{}"),
        });

        var snapshots = await CreateProvider(handler)
            .PullListingsAsync(Credentials, CancellationToken.None);

        snapshots.Should().HaveCount(1);
        snapshots[0].ExternalListingId.Should().Be("1");
        snapshots[0].Title.Should().Be("Seaside apartment");
        snapshots[0].NightlyRateCents.Should().BeNull();
    }

    [Fact]
    public async Task PullListingsAsync_WithoutSecret_MakesNoRequests()
    {
        var handler = new StubHandler(_ => Ok(ApartmentsIndex));
        var credentials = new ChannelCredentials("smoobu", ApiKey);

        var snapshots = await CreateProvider(handler)
            .PullListingsAsync(credentials, CancellationToken.None);

        snapshots.Should().BeEmpty();
        handler.Calls.Should().BeEmpty();
    }

    // ── HMAC signing ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Requests_CarryValidHmacHeaders()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/api/apartments" => Ok("""{"apartments":[]}"""),
            _ => (HttpStatusCode.NotFound, "{}"),
        });

        await CreateProvider(handler).PullListingsAsync(Credentials, CancellationToken.None);

        var call = handler.Calls.Single();
        call.Headers.Should().ContainKey("X-API-Key").WhoseValue.Should().Be(ApiKey);
        call.Headers.Should().ContainKey("X-Timestamp");
        call.Headers.Should().ContainKey("X-Nonce");
        call.Headers.Should().ContainKey("X-Signature");

        // Recompute the signature exactly as the Smoobu docs describe:
        // METHOD\nPATH\nSORTED_QUERY\nTIMESTAMP\nNONCE\nSHA256(body)\nAPI_KEY.
        var emptyBodyHash = Convert.ToHexString(SHA256.HashData(Array.Empty<byte>()))
            .ToLowerInvariant();
        var canonical = string.Join('\n',
            "GET",
            "/api/apartments",
            "",
            call.Headers["X-Timestamp"],
            call.Headers["X-Nonce"],
            emptyBodyHash,
            ApiKey);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret));
        var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));

        call.Headers["X-Signature"].Should().Be(expected);
    }

    [Fact]
    public async Task Requests_WithQuery_SignSortedQueryString()
    {
        var handler = new StubHandler(_ => Ok("""{"data":{}}"""));

        await CreateProvider(handler)
            .PullAvailabilityAsync(Credentials, "42", CancellationToken.None);

        var call = handler.Calls.Single();
        call.Path.Should().Be("/api/rates");

        var sortedQuery = string.Join('&', call.Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(p => p, StringComparer.Ordinal));
        var emptyBodyHash = Convert.ToHexString(SHA256.HashData(Array.Empty<byte>()))
            .ToLowerInvariant();
        var canonical = string.Join('\n',
            "GET",
            "/api/rates",
            sortedQuery,
            call.Headers["X-Timestamp"],
            call.Headers["X-Nonce"],
            emptyBodyHash,
            ApiKey);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ApiSecret));
        var expected = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));

        call.Headers["X-Signature"].Should().Be(expected);
    }

    // ── Availability ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PullAvailabilityAsync_CollapsesRateDaysToBlocks()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var handler = new StubHandler(_ => Ok($$"""
            {"data":{"42":{
              "{{Iso(today)}}": {"price": 100, "available": 1},
              "{{Iso(today.AddDays(1))}}": {"price": 100, "available": 1},
              "{{Iso(today.AddDays(2))}}": {"price": null, "available": 0},
              "{{Iso(today.AddDays(3))}}": {"price": 100, "available": 1}
            }
            }
            }
            """));

        var calendar = await CreateProvider(handler)
            .PullAvailabilityAsync(Credentials, "42", CancellationToken.None);

        calendar.Blocks.Should().HaveCount(3);
        IsAvailable(calendar, today).Should().BeTrue();
        IsAvailable(calendar, today.AddDays(1)).Should().BeTrue();
        IsAvailable(calendar, today.AddDays(2)).Should().BeFalse();
        IsAvailable(calendar, today.AddDays(3)).Should().BeTrue();

        handler.Calls[0].Query.Should().Contain("apartments%5B%5D=42");
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ApartmentListed_ReturnsAvailable()
    {
        var handler = new StubHandler(request =>
            (request.Method.Method, request.RequestUri!.AbsolutePath) switch
            {
                ("GET", "/api/me") => Ok("""{"id":9,"firstName":"John","lastName":"Doe"}"""),
                ("POST", "/booking/checkApartmentAvailability") => Ok("""
                    {"availableApartments":[42],"prices":{"42":{"price":500,"currency":"EUR"}}}
                    """),
                _ => (HttpStatusCode.NotFound, "{}"),
            });
        var query = new ChannelAvailabilityQuery(
            "42", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5), Adults: 2, Children: 1);

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeTrue();

        var body = handler.Calls
            .Single(c => c.Path == "/booking/checkApartmentAvailability").Body!;
        body.Should().Contain("\"arrivalDate\":\"2026-09-01\"");
        body.Should().Contain("\"departureDate\":\"2026-09-05\"");
        body.Should().Contain("\"apartments\":[42]");
        body.Should().Contain("\"customerId\":9");
        body.Should().Contain("\"guests\":3");
    }

    [Fact]
    public async Task CheckAvailabilityAsync_ApartmentNotListed_ReturnsUnavailable()
    {
        var handler = new StubHandler(request =>
            (request.Method.Method, request.RequestUri!.AbsolutePath) switch
            {
                ("GET", "/api/me") => Ok("""{"id":9}"""),
                ("POST", "/booking/checkApartmentAvailability") => Ok("""
                    {"availableApartments":[],
                     "errorMessages":{"42":{"errorCode":401,"message":"The duration of the booking is too short."}}}
                    """),
                _ => (HttpStatusCode.NotFound, "{}"),
            });
        var query = new ChannelAvailabilityQuery(
            "42", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 2));

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeFalse();
        result.ErrorCode.Should().Be("Unavailable");
    }

    [Fact]
    public async Task CheckAvailabilityAsync_InvertedDates_ShortCircuits()
    {
        var handler = new StubHandler(_ => Ok("{}"));
        var query = new ChannelAvailabilityQuery(
            "42", new DateOnly(2026, 9, 5), new DateOnly(2026, 9, 1));

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidDates");
        handler.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAvailabilityAsync_NonNumericListingId_ShortCircuits()
    {
        var handler = new StubHandler(_ => Ok("{}"));
        var query = new ChannelAvailabilityQuery(
            "sb-42", new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 5));

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidListingId");
        handler.Calls.Should().BeEmpty();
    }

    // ── Booking push ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PushBookingAsync_SendsPaidReservationAndReturnsId()
    {
        var handler = new StubHandler(request =>
            (request.Method.Method, request.RequestUri!.AbsolutePath) switch
            {
                ("POST", "/api/reservations") => Ok("""{"id":906}"""),
                _ => (HttpStatusCode.NotFound, "{}"),
            });

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, SampleBooking(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExternalBookingId.Should().Be("906");

        var body = handler.Calls.Single().Body!;
        body.Should().Contain("\"apartmentId\":42");
        body.Should().Contain("\"channelId\":70");
        body.Should().Contain("\"arrivalDate\":\"2026-09-01\"");
        body.Should().Contain("\"departureDate\":\"2026-09-08\"");
        body.Should().Contain("\"firstName\":\"Ada\"");
        body.Should().Contain("\"email\":\"ada@example.com\"");
        body.Should().Contain("\"price\":1400");
        body.Should().Contain("\"priceStatus\":1");
        body.Should().Contain("LGD-TRACK-1");
    }

    [Fact]
    public async Task PushBookingAsync_RejectedBySmoobu_Fails()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.BadRequest,
            """{"status":400,"title":"Bad Request","detail":"Apartment not found"}"""));

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, SampleBooking(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("RequestFailed");
    }

    [Fact]
    public async Task PushBookingAsync_NonNumericListingId_FailsWithoutCallingApi()
    {
        var handler = new StubHandler(_ => Ok("{}"));
        var request = SampleBooking() with { ExternalListingId = "sb-42" };

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, request, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidListingId");
        handler.Calls.Should().BeEmpty();
    }

    // ── Booking updates ──────────────────────────────────────────────────────

    [Fact]
    public async Task PullBookingUpdatesAsync_MapsTypesAndSkipsBlockedBookings()
    {
        var handler = new StubHandler(_ => Ok("""
            {"page_count":1,"page_size":100,"total_items":3,"page":1,"bookings":[
              {"id":9001,"type":"reservation","modifiedAt":"2026-07-20 10:00","is-blocked-booking":false},
              {"id":9002,"type":"cancellation","modifiedAt":"2026-07-21 11:30","is-blocked-booking":false},
              {"id":9003,"type":"reservation","modifiedAt":"2026-07-22 09:00","is-blocked-booking":true}
            ]}
            """));

        var updates = await CreateProvider(handler).PullBookingUpdatesAsync(
            Credentials, new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        updates.Should().HaveCount(2);
        updates.Select(u => (u.ExternalBookingId, u.Status)).Should().Equal(
            ("9001", "confirmed"),
            ("9002", "cancelled"));
        updates[0].ChangedAtUtc.Should().Be(new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc));

        handler.Calls[0].Query.Should().Contain("modifiedFrom=2026-07-19");
        handler.Calls[0].Query.Should().Contain("showCancellation=true");
    }

    [Fact]
    public async Task PullBookingUpdatesAsync_FollowsPagesUntilPageCount()
    {
        var handler = new StubHandler(request =>
            request.RequestUri!.Query.Contains("page=2", StringComparison.Ordinal)
                ? Ok("""
                    {"page_count":2,"page":2,"bookings":[
                      {"id":9002,"type":"reservation","modifiedAt":"2026-07-21 11:30"}]}
                    """)
                : Ok("""
                    {"page_count":2,"page":1,"bookings":[
                      {"id":9001,"type":"reservation","modifiedAt":"2026-07-20 10:00"}]}
                    """));

        var updates = await CreateProvider(handler).PullBookingUpdatesAsync(
            Credentials, DateTime.UtcNow.AddDays(-7), CancellationToken.None);

        updates.Select(u => u.ExternalBookingId).Should().Equal("9001", "9002");
        handler.Calls.Should().HaveCount(2);
    }

    [Fact]
    public async Task PullBookingUpdatesAsync_ApiFailure_ReturnsEmpty()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Unauthorized,
            """{"status":401,"title":"Unauthorized","detail":"Authentication required"}"""));

        var updates = await CreateProvider(handler).PullBookingUpdatesAsync(
            Credentials, DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        updates.Should().BeEmpty();
    }

    // ── Fixtures / helpers ───────────────────────────────────────────────────

    private const string ApartmentsIndex = """
        {"apartments":[{"id":1,"name":"Seaside apartment"}]}
        """;

    private const string ApartmentDetail = """
        {
          "location": {
            "street": "Wönnichstr. 68/70",
            "zip": "10317",
            "city": "Berlin",
            "country": "Germany",
            "latitude": "52.5200080000000",
            "longitude": "13.4049540000000"
          },
          "timeZone": "Europe/Berlin",
          "rooms": {
            "maxOccupancy": 4,
            "bedrooms": 4,
            "bathrooms": 2,
            "doubleBeds": 1,
            "singleBeds": 3
          },
          "equipments": ["Internet", "Pool", "Heating"],
          "currency": "EUR",
          "price": { "minimal": "85.00", "maximal": "100.00" },
          "type": { "id": 2, "name": "Holiday rental" }
        }
        """;

    private static ChannelBookingPushRequest SampleBooking() => new(
        ExternalListingId: "42",
        Guest: new ChannelGuest("Ada", "Lovelace", "ada@example.com", "+15551234567"),
        CheckIn: new DateOnly(2026, 9, 1),
        CheckOut: new DateOnly(2026, 9, 8),
        Adults: 2,
        Children: 1,
        Pets: 0,
        Currency: "EUR",
        OrderItems:
        [
            new ChannelOrderItem("rent", "Nightly rent", 120_000),
            new ChannelOrderItem("cleaning", "Cleaning fee", 20_000),
        ],
        PaymentStatus: "paid",
        TrackingReference: "LGD-TRACK-1");

    private static (HttpStatusCode, string) Ok(string body) => (HttpStatusCode.OK, body);

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
