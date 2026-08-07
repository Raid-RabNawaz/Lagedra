using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Lagedra.Tests.Unit.ChannelIntegration.Infrastructure;

/// <summary>
/// Exercises the OwnerRez API v2 provider against canned v2 payloads so the
/// request shapes, Basic auth, paging, and response mapping are all pinned.
/// </summary>
public sealed class OwnerRezChannelProviderTests
{
    private const int PropertyId = 4321;

    private static readonly ChannelCredentials Credentials = new(
        ProviderKey: "ownerrez",
        ExternalAccountId: "host@example.com",
        Username: "host@example.com",
        Secret: "pt_live_token");

    private static OwnerRezChannelProvider CreateProvider(
        StubHandler handler,
        OwnerRezChannelSettings? settings = null)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.ownerrez.com") },
            Options.Create(settings ?? new OwnerRezChannelSettings()),
            NullLogger<OwnerRezChannelProvider>.Instance);

    // ── Listing import ───────────────────────────────────────────────────────

    [Fact]
    public async Task PullListingsAsync_MergesPropertyAndListingContent()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/v2/properties" => Ok(PropertiesPage),
            "/v2/listings" => Ok(ListingsPage),
            _ => (HttpStatusCode.NotFound, "{}"),
        });

        var snapshots = await CreateProvider(handler)
            .PullListingsAsync(Credentials, CancellationToken.None);

        snapshots.Should().HaveCount(1);
        var snapshot = snapshots[0];

        snapshot.ExternalListingId.Should().Be("4321");
        snapshot.Title.Should().Be("Harbour Loft");
        snapshot.Description.Should().Be("Bright two-bedroom loft steps from the seawall.");
        snapshot.Currency.Should().Be("CAD");
        snapshot.NightlyRateCents.Should().Be(18_950);
        snapshot.MonthlyRentCents.Should().Be(568_500);
        snapshot.Bedrooms.Should().Be(2);
        // 1 full + 1 half bathroom, not the flat count of 2 that OwnerRez reports.
        snapshot.Bathrooms.Should().Be(1.5m);
        // 90 m² converted to square feet.
        snapshot.SquareFootage.Should().Be(969);
        snapshot.Latitude.Should().BeApproximately(49.2827, 0.0001);
        snapshot.Longitude.Should().BeApproximately(-123.1207, 0.0001);
        snapshot.PropertyType.Should().Be("condo");
        snapshot.DepositCents.Should().BeNull();

        snapshot.Address.Should().NotBeNull();
        snapshot.Address!.Line1.Should().Be("12 Water St");
        snapshot.Address.City.Should().Be("Vancouver");
        snapshot.Address.State.Should().Be("BC");
        snapshot.Address.PostalCode.Should().Be("V6B 1A1");
        snapshot.Address.Country.Should().Be("CA");

        // The third photo has no usable URL and is dropped.
        var photos = snapshot.Photos.Should().NotBeNull().And.HaveCount(2).And.Subject.ToList();
        photos.Select(p => p.ExternalId).Should().Equal("4321-0", "4321-1");
        photos[0].Url.Should().Be(new Uri("https://cdn.ownerrez.com/p/4321-1.jpg"));
        photos[0].Caption.Should().Be("Living room");

        // Amenities come from both collections and de-duplicate.
        snapshot.AmenityCodes.Should().BeEquivalentTo(
            new[] { "Dishwasher", "Coffee maker", "Free parking" },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task PullListingsAsync_SendsBasicAuthFromEmailAndToken()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/v2/properties" => Ok(PropertiesPage),
            "/v2/listings" => Ok(ListingsPage),
            _ => (HttpStatusCode.NotFound, "{}"),
        });

        await CreateProvider(handler).PullListingsAsync(Credentials, CancellationToken.None);

        var expected = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("host@example.com:pt_live_token"));
        handler.Calls.Should().OnlyContain(c => c.Authorization == $"Basic {expected}");
        handler.Calls[0].Query.Should().Contain("active=true");
    }

    [Fact]
    public async Task PullListingsAsync_TokenOnlyConnection_UsesTokenAsBasicUsername()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/v2/properties" => Ok(PropertiesPage),
            "/v2/listings" => Ok(ListingsPage),
            _ => (HttpStatusCode.NotFound, "{}"),
        });
        var credentials = new ChannelCredentials("ownerrez", "4321", Username: null, Secret: "pt_key_only");

        await CreateProvider(handler).PullListingsAsync(credentials, CancellationToken.None);

        var expected = Convert.ToBase64String(Encoding.UTF8.GetBytes("pt_key_only:"));
        handler.Calls.Should().OnlyContain(c => c.Authorization == $"Basic {expected}");
    }

    [Fact]
    public async Task PullListingsAsync_OAuthToken_SendsBearerAuth()
    {
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/v2/properties" => Ok(PropertiesPage),
            "/v2/listings" => Ok(ListingsPage),
            _ => (HttpStatusCode.NotFound, "{}"),
        });
        var credentials = new ChannelCredentials(
            "ownerrez", "123456", Username: null, Secret: "at_host_token");

        await CreateProvider(handler).PullListingsAsync(credentials, CancellationToken.None);

        handler.Calls.Should().OnlyContain(c => c.Authorization == "bearer at_host_token");
    }

    /// <summary>
    /// A refused sync must not be reported to the host as "you have no
    /// properties" — the two are indistinguishable to them otherwise.
    /// </summary>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "access token is still active")]
    [InlineData(HttpStatusCode.Forbidden, "denied access")]
    [InlineData((HttpStatusCode)429, "rate limiting")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "temporarily unavailable")]
    public async Task PullListingsAsync_RejectedRequest_ThrowsWithActionableMessage(
        HttpStatusCode status,
        string expectedFragment)
    {
        var handler = new StubHandler(_ => (status, """{"messages":["nope"]}"""));

        var pull = async () => await CreateProvider(handler)
            .PullListingsAsync(Credentials, CancellationToken.None);

        (await pull.Should().ThrowAsync<HttpRequestException>())
            .WithMessage($"*{expectedFragment}*");
    }

    /// <summary>
    /// A host whose token is fine but who tripped OwnerRez's cap of two accounts per
    /// address per day would otherwise be sent to re-check a token that is not the
    /// problem, so the refusal names the cap as a possible cause.
    /// </summary>
    [Fact]
    public async Task PullListingsAsync_ForbiddenPersonalToken_MentionsAccountCap()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Forbidden, "{}"));

        var pull = async () => await CreateProvider(handler)
            .PullListingsAsync(Credentials, CancellationToken.None);

        (await pull.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("*two accounts per day*");
    }

    [Fact]
    public async Task PullListingsAsync_RejectedOAuthToken_SaysReconnect()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Unauthorized, "{}"));
        var credentials = new ChannelCredentials(
            "ownerrez", "123456", Username: null, Secret: "at_host_token");

        var pull = async () => await CreateProvider(handler)
            .PullListingsAsync(credentials, CancellationToken.None);

        (await pull.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("*connect OwnerRez again*");
    }

    [Fact]
    public async Task PullListingsAsync_ExpiredOAuthToken_SaysAuthorizationExpired()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Unauthorized, "{}"))
        {
            WwwAuthenticate = ("Bearer", "error=\"token_expired\""),
        };

        var pull = async () => await CreateProvider(handler)
            .PullListingsAsync(Credentials, CancellationToken.None);

        (await pull.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("*authorization has expired*");
    }

    /// <summary>
    /// Background pulls stay tolerant: one unavailable channel must not take a
    /// scheduled job down with it.
    /// </summary>
    [Fact]
    public async Task PullAvailabilityAsync_RejectedRequest_ReturnsEmptyWithoutThrowing()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Unauthorized, "{}"));

        var calendar = await CreateProvider(handler)
            .PullAvailabilityAsync(Credentials, "4321", CancellationToken.None);

        calendar.Blocks.Should().OnlyContain(b => b.Available);
    }

    [Fact]
    public async Task PullListingsAsync_WithoutToken_MakesNoRequests()
    {
        var handler = new StubHandler(_ => Ok(PropertiesPage));
        var credentials = new ChannelCredentials("ownerrez", "host@example.com");

        var snapshots = await CreateProvider(handler)
            .PullListingsAsync(credentials, CancellationToken.None);

        snapshots.Should().BeEmpty();
        handler.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task PullListingsAsync_FollowsNextPageUrlUntilExhausted()
    {
        const string page2 = "https://api.ownerrez.com/v2/properties?active=true&limit=1&offset=1";
        var handler = new StubHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/v2/listings")
            {
                return Ok("""{"items":[],"next_page_url":null}""");
            }

            return request.RequestUri.Query.Contains("offset=1", StringComparison.Ordinal)
                ? Ok($$"""
                    {"items":[{"id":99,"name":"Second","active":true}],"next_page_url":null}
                    """)
                : Ok($$"""
                    {"items":[{"id":98,"name":"First","active":true}],"next_page_url":"{{page2}}"}
                    """);
        });

        var snapshots = await CreateProvider(handler, new OwnerRezChannelSettings { PageSize = 1 })
            .PullListingsAsync(Credentials, CancellationToken.None);

        snapshots.Select(s => s.ExternalListingId).Should().Equal("98", "99");
        handler.Calls.Count(c => c.Path == "/v2/properties").Should().Be(2);
    }

    // ── Availability ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PullAvailabilityAsync_MarksBookedNightsUnavailable()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var arrival = today.AddDays(10);
        var departure = today.AddDays(13);

        var handler = new StubHandler(_ => Ok($$"""
            {"items":[{"id":1,"arrival":"{{Iso(arrival)}}","departure":"{{Iso(departure)}}",
              "status":"active","is_block":false}],"next_page_url":null}
            """));

        var calendar = await CreateProvider(handler)
            .PullAvailabilityAsync(Credentials, "4321", CancellationToken.None);

        IsAvailable(calendar, today).Should().BeTrue();
        IsAvailable(calendar, arrival).Should().BeFalse();
        IsAvailable(calendar, arrival.AddDays(1)).Should().BeFalse();
        IsAvailable(calendar, arrival.AddDays(2)).Should().BeFalse();
        // Departure day is a turnover day, so that night is bookable again.
        IsAvailable(calendar, departure).Should().BeTrue();

        handler.Calls[0].Query.Should().Contain($"property_ids={PropertyId}");
        handler.Calls[0].Query.Should().Contain("status=active");
    }

    [Fact]
    public async Task PullAvailabilityAsync_NonNumericListingId_ReturnsEmpty()
    {
        var handler = new StubHandler(_ => Ok("""{"items":[]}"""));

        var calendar = await CreateProvider(handler)
            .PullAvailabilityAsync(Credentials, "ora-12345", CancellationToken.None);

        calendar.Blocks.Should().BeEmpty();
        handler.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAvailabilityAsync_NoRuleViolations_ReturnsAvailable()
    {
        var handler = new StubHandler(_ => Ok("""
            {"items":[{"id":4321,"name":"Harbour Loft","rule_violations":[]}],"count":1}
            """));
        var query = new ChannelAvailabilityQuery(
            "4321", new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 15), Adults: 2, Pets: 1);

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeTrue();
        result.ErrorCode.Should().BeNull();

        var sent = handler.Calls[0];
        sent.Path.Should().Be("/v2/propertysearch");
        sent.Query.Should().Contain("available_from=2026-08-10");
        sent.Query.Should().Contain("available_to=2026-08-15");
        sent.Query.Should().Contain("evaluate_rules=true");
        sent.Query.Should().Contain("pets_allowed=true");
    }

    [Fact]
    public async Task CheckAvailabilityAsync_RuleViolation_ReturnsUnavailableWithReason()
    {
        var handler = new StubHandler(_ => Ok("""
            {"items":[{"id":4321,"rule_violations":["Minimum stay of 3 nights"]}],"count":1}
            """));
        var query = new ChannelAvailabilityQuery(
            "4321", new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 11));

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeFalse();
        result.ErrorCode.Should().Be("RuleViolation");
    }

    [Fact]
    public async Task CheckAvailabilityAsync_PropertyNotReturned_ReturnsUnavailable()
    {
        var handler = new StubHandler(_ => Ok("""{"items":[],"count":0}"""));
        var query = new ChannelAvailabilityQuery(
            "4321", new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 15));

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeFalse();
        result.ErrorCode.Should().Be("Unavailable");
    }

    [Fact]
    public async Task CheckAvailabilityAsync_InvertedDates_ShortCircuits()
    {
        var handler = new StubHandler(_ => Ok("""{"items":[]}"""));
        var query = new ChannelAvailabilityQuery(
            "4321", new DateOnly(2026, 8, 15), new DateOnly(2026, 8, 10));

        var result = await CreateProvider(handler)
            .CheckAvailabilityAsync(Credentials, query, CancellationToken.None);

        result.Available.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidDates");
        handler.Calls.Should().BeEmpty();
    }

    // ── Booking push ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PushBookingAsync_ReusesExistingGuestAndReturnsBookingId()
    {
        var handler = new StubHandler(request =>
            (request.Method.Method, request.RequestUri!.AbsolutePath) switch
            {
                ("GET", "/v2/guests") => Ok("""
                    {"items":[{"id":77,"first_name":"Ada","last_name":"Lovelace",
                      "email_addresses":[{"address":"ada@example.com","is_default":true}]}]}
                    """),
                ("POST", "/v2/bookings") => Ok("""{"id":9001,"status":"active"}"""),
                _ => (HttpStatusCode.NotFound, "{}"),
            });

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, SampleBooking(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExternalBookingId.Should().Be("9001");

        handler.Calls.Should().NotContain(c => c.Method == "POST" && c.Path == "/v2/guests");

        var booking = handler.Calls.Single(c => c.Path == "/v2/bookings").Body!;
        booking.Should().Contain("\"property_id\":4321");
        booking.Should().Contain("\"guest_id\":77");
        booking.Should().Contain("\"arrival\":\"2026-09-01\"");
        booking.Should().Contain("\"departure\":\"2026-09-08\"");
        booking.Should().Contain("\"is_block\":false");
        // v2 bookings carry no charges, so the breakdown lands in notes.
        booking.Should().Contain("LGD-TRACK-1");
        booking.Should().Contain("Total USD 1400.00");
        booking.Should().Contain("Owner commission USD 100.00");
    }

    [Fact]
    public async Task PushBookingAsync_UnknownGuest_CreatesGuestFirst()
    {
        var handler = new StubHandler(request =>
            (request.Method.Method, request.RequestUri!.AbsolutePath) switch
            {
                ("GET", "/v2/guests") => Ok("""{"items":[]}"""),
                ("POST", "/v2/guests") => Ok("""{"id":88}"""),
                ("POST", "/v2/bookings") => Ok("""{"id":9002}"""),
                _ => (HttpStatusCode.NotFound, "{}"),
            });

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, SampleBooking(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ExternalBookingId.Should().Be("9002");

        var created = handler.Calls.Single(c => c.Method == "POST" && c.Path == "/v2/guests").Body!;
        created.Should().Contain("ada@example.com");
        created.Should().Contain("Ada");
        // System.Text.Json escapes the leading "+" of the phone number.
        created.Should().Contain("15551234567").And.Contain("\"type\":\"mobile\"");

        handler.Calls.Single(c => c.Path == "/v2/bookings").Body!
            .Should().Contain("\"guest_id\":88");
    }

    [Fact]
    public async Task PushBookingAsync_RejectedByOwnerRez_SurfacesApiMessages()
    {
        var handler = new StubHandler(request =>
            (request.Method.Method, request.RequestUri!.AbsolutePath) switch
            {
                ("GET", "/v2/guests") => Ok("""
                    {"items":[{"id":77,"email_addresses":[{"address":"ada@example.com"}]}]}
                    """),
                ("POST", "/v2/bookings") => (HttpStatusCode.BadRequest,
                    """{"messages":["Those dates are not available."]}"""),
                _ => (HttpStatusCode.NotFound, "{}"),
            });

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, SampleBooking(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("RequestFailed");
        result.ErrorMessage.Should().Be("Those dates are not available.");
    }

    [Fact]
    public async Task PushBookingAsync_NonNumericListingId_FailsWithoutCallingApi()
    {
        var handler = new StubHandler(_ => Ok("{}"));
        var request = SampleBooking() with { ExternalListingId = "ora-12345" };

        var result = await CreateProvider(handler)
            .PushBookingAsync(Credentials, request, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be("InvalidListingId");
        handler.Calls.Should().BeEmpty();
    }

    // ── Booking updates ──────────────────────────────────────────────────────

    [Fact]
    public async Task PullBookingUpdatesAsync_MapsStatusesAndSkipsBlocks()
    {
        var handler = new StubHandler(_ => Ok("""
            {"items":[
              {"id":9001,"status":"active","is_block":false,"updated_utc":"2026-07-20T10:00:00Z"},
              {"id":9002,"status":"canceled","is_block":false,"updated_utc":"2026-07-21T11:30:00Z"},
              {"id":9003,"status":"pending","is_block":false,"booked_utc":"2026-07-22T09:00:00Z"},
              {"id":9004,"status":"active","is_block":true,"updated_utc":"2026-07-23T09:00:00Z"}
            ],"next_page_url":null}
            """));

        var updates = await CreateProvider(handler).PullBookingUpdatesAsync(
            Credentials, new DateTime(2026, 7, 19, 0, 0, 0, DateTimeKind.Utc), CancellationToken.None);

        updates.Should().HaveCount(3);
        updates.Select(u => (u.ExternalBookingId, u.Status)).Should().Equal(
            ("9001", "confirmed"),
            ("9002", "cancelled"),
            ("9003", "pending"));
        updates[0].ChangedAtUtc.Should().Be(new DateTime(2026, 7, 20, 10, 0, 0, DateTimeKind.Utc));
        updates[2].ChangedAtUtc.Should().Be(new DateTime(2026, 7, 22, 9, 0, 0, DateTimeKind.Utc));

        handler.Calls[0].Query.Should().Contain("since_utc=2026-07-19T00%3A00%3A00Z");
    }

    [Fact]
    public async Task PullBookingUpdatesAsync_ApiFailure_ReturnsEmpty()
    {
        var handler = new StubHandler(_ => (HttpStatusCode.Unauthorized, """{"messages":["Bad token"]}"""));

        var updates = await CreateProvider(handler).PullBookingUpdatesAsync(
            Credentials, DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        updates.Should().BeEmpty();
    }

    // ── Fixtures / helpers ───────────────────────────────────────────────────

    private const string PropertiesPage = """
        {
          "items": [{
            "id": 4321,
            "name": "Harbour Loft",
            "external_name": "Harbour Loft - Downtown",
            "active": true,
            "bedrooms": 2,
            "bathrooms": 2,
            "bathrooms_full": 1,
            "bathrooms_half": 1,
            "currency_code": "CAD",
            "latitude": 49.2827,
            "longitude": -123.1207,
            "living_area": 90,
            "living_area_type": "m\u00B2",
            "property_type": "condo",
            "address": {
              "street1": "12 Water St",
              "city": "Vancouver",
              "state": "BC",
              "postal_code": "V6B 1A1",
              "country": "CA"
            }
          }],
          "limit": 100,
          "offset": 0,
          "count": 1,
          "next_page_url": null
        }
        """;

    private const string ListingsPage = """
        {
          "items": [{
            "property_id": 4321,
            "bedroom_count": 2,
            "bathroom_count": 2,
            "nightly_rate_min": 189.50,
            "nightly_rate_max": 320.00,
            "descriptions": {
              "headline": "Loft with harbour views",
              "description": "Bright two-bedroom loft steps from the seawall."
            },
            "photos": [
              { "original_url": "https://cdn.ownerrez.com/p/4321-1.jpg", "caption": "Living room" },
              { "large_url": "https://cdn.ownerrez.com/p/4321-2.jpg" },
              { "caption": "no usable url" }
            ],
            "amenity_categories": [{
              "caption": "Kitchen",
              "amenities": [{ "title": "Dishwasher" }, { "text": "Coffee maker" }]
            }],
            "amenity_call_outs": [{ "title": "Dishwasher" }, { "text": "Free parking" }]
          }],
          "limit": 100,
          "offset": 0,
          "next_page_url": null
        }
        """;

    private static ChannelBookingPushRequest SampleBooking() => new(
        ExternalListingId: "4321",
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
        TrackingReference: "LGD-TRACK-1",
        OwnerCommissionCents: 10_000,
        GuestServiceFeeCents: 5_000);

    private static (HttpStatusCode, string) Ok(string body) => (HttpStatusCode.OK, body);

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static bool IsAvailable(ChannelAvailabilityCalendar calendar, DateOnly date)
        => calendar.Blocks.Single(b => date >= b.Start && date <= b.End).Available;

    private sealed record Call(string Method, string Path, string Query, string? Authorization, string? Body);

    private sealed class StubHandler(Func<HttpRequestMessage, (HttpStatusCode Status, string Body)> router)
        : HttpMessageHandler
    {
        public List<Call> Calls { get; } = [];

        /// <summary>Set to reproduce OwnerRez's expired-token challenge header.</summary>
        public (string Scheme, string Parameter)? WwwAuthenticate { get; init; }

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
                request.Headers.Authorization?.ToString(),
                body));

            var (status, responseBody) = router(request);
            var response = new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
            };

            if (WwwAuthenticate is { } challenge)
            {
                response.Headers.WwwAuthenticate.Add(
                    new AuthenticationHeaderValue(challenge.Scheme, challenge.Parameter));
            }

            return response;
        }
    }
}
