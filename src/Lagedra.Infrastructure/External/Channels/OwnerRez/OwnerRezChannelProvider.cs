using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>
/// OwnerRez implementation of <see cref="IChannelProvider"/> against the OwnerRez
/// API v2 (REST/JSON, <c>https://api.ownerrez.com/v2/…</c>). Per-connection
/// credentials are the host's OwnerRez account email
/// (<see cref="ChannelCredentials.Username"/>) and personal access token
/// (<see cref="ChannelCredentials.Secret"/>), sent as HTTP Basic on every call.
///
/// v2 has no merchant-of-record booking form: <c>BookingEditModel</c> carries no
/// money fields and payments are read-only, so a pushed booking records the
/// stay and guest only. The priced breakdown Lagedra collected is written into
/// the booking notes so the host can reconcile it.
/// </summary>
public sealed partial class OwnerRezChannelProvider(
    HttpClient httpClient,
    IOptions<OwnerRezChannelSettings> settings,
    ILogger<OwnerRezChannelProvider> logger) : IChannelProvider
{
    private readonly OwnerRezChannelSettings _settings = settings.Value;

    public string ProviderKey => "ownerrez";

    public async Task<IReadOnlyList<ChannelListingSnapshot>> PullListingsAsync(
        ChannelCredentials credentials,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (!EnsureCredentials(credentials, nameof(PullListingsAsync)))
        {
            return [];
        }

        // This is the sync the host watches, so a refusal is reported rather than
        // swallowed: "no properties yet" and "your authorization expired" must not
        // look the same on the Channels page.
        var properties = await ReadAllPagesAsync(
                credentials,
                "/v2/properties?active=true",
                ParseProperty,
                ct,
                reportFailure: true)
            .ConfigureAwait(false);
        if (properties.Count == 0)
        {
            return [];
        }

        // Listing content (descriptions, photos, amenities, rate range) is a
        // separate resource keyed by property_id.
        var content = await ReadAllPagesAsync(
                credentials,
                "/v2/listings?includeAmenities=true&includeImages=true&includeRooms=true"
                + "&includeBathrooms=true&includeDescriptions=text",
                ParseListingContent,
                ct)
            .ConfigureAwait(false);
        var contentByPropertyId = content
            .GroupBy(c => c.PropertyId)
            .ToDictionary(g => g.Key, g => g.First());

        return properties
            .Select(p => BuildSnapshot(p, contentByPropertyId.GetValueOrDefault(p.Id)))
            .ToList();
    }

    public async Task<ChannelAvailabilityCalendar> PullAvailabilityAsync(
        ChannelCredentials credentials,
        string externalListingId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var empty = new ChannelAvailabilityCalendar(externalListingId, []);
        if (!EnsureCredentials(credentials, nameof(PullAvailabilityAsync))
            || !TryParsePropertyId(externalListingId, out var propertyId))
        {
            return empty;
        }

        var start = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var end = start.AddDays(Math.Max(30, _settings.AvailabilityLookaheadDays));

        var booked = await FetchBookedNightsAsync(credentials, propertyId, start, end, ct)
            .ConfigureAwait(false);

        var days = new Dictionary<DateOnly, bool>();
        for (var day = start; day <= end; day = day.AddDays(1))
        {
            days[day] = !booked.Contains(day);
        }

        return new ChannelAvailabilityCalendar(externalListingId, CollapseToBlocks(days));
    }

    public async Task<ChannelAvailabilityResult> CheckAvailabilityAsync(
        ChannelCredentials credentials,
        ChannelAvailabilityQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(query);
        if (!EnsureCredentials(credentials, nameof(CheckAvailabilityAsync)))
        {
            return new ChannelAvailabilityResult(false, "NotConfigured");
        }

        if (query.CheckOut <= query.CheckIn)
        {
            return new ChannelAvailabilityResult(false, "InvalidDates");
        }

        if (!TryParsePropertyId(query.ExternalListingId, out var propertyId))
        {
            return new ChannelAvailabilityResult(false, "InvalidListingId");
        }

        // propertysearch evaluates OwnerRez's own availability + booking rules
        // (min/max nights, changeover days, advance notice) for the window.
        var path =
            $"/v2/propertysearch?property_ids={propertyId}"
            + $"&available_from={Date(query.CheckIn)}&available_to={Date(query.CheckOut)}"
            + "&evaluate_rules=true";
        if (query.Pets > 0)
        {
            path += "&pets_allowed=true";
        }

        using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
        if (doc is null)
        {
            return new ChannelAvailabilityResult(false, "RequestFailed");
        }

        if (!doc.RootElement.TryGetProperty("items", out var items)
            || items.ValueKind != JsonValueKind.Array)
        {
            return new ChannelAvailabilityResult(false, "RequestFailed");
        }

        foreach (var item in items.EnumerateArray())
        {
            if (Int(item, "id") != propertyId)
            {
                continue;
            }

            if (item.TryGetProperty("rule_violations", out var violations)
                && violations.ValueKind == JsonValueKind.Array
                && violations.GetArrayLength() > 0)
            {
                return new ChannelAvailabilityResult(false, "RuleViolation");
            }

            return new ChannelAvailabilityResult(true);
        }

        return new ChannelAvailabilityResult(false, "Unavailable");
    }

    public async Task<ChannelBookingPushResult> PushBookingAsync(
        ChannelCredentials credentials,
        ChannelBookingPushRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(request);
        if (!HasCredentials(credentials))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "NotConfigured",
                ErrorMessage: "OwnerRez account credentials are not configured on this connection.");
        }

        if (!TryParsePropertyId(request.ExternalListingId, out var propertyId))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "InvalidListingId",
                ErrorMessage: "OwnerRez property id must be numeric.");
        }

        var guestId = await ResolveGuestIdAsync(credentials, request.Guest, ct).ConfigureAwait(false);
        if (guestId is null)
        {
            return new ChannelBookingPushResult(false, ErrorCode: "GuestFailed",
                ErrorMessage: "Could not find or create the guest in OwnerRez.");
        }

        var body = new Dictionary<string, object?>
        {
            ["property_id"] = propertyId,
            ["guest_id"] = guestId.Value,
            ["arrival"] = Date(request.CheckIn),
            ["departure"] = Date(request.CheckOut),
            ["is_block"] = false,
            ["notes"] = BuildBookingNotes(request),
        };

        try
        {
            using var response = await SendAsync(
                    credentials, HttpMethod.Post, "/v2/bookings", JsonContent.Create(body), ct)
                .ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, nameof(PushBookingAsync), (int)response.StatusCode, "/v2/bookings");
                return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                    ErrorMessage: ExtractErrorMessage(payload) ?? "OwnerRez rejected the booking.");
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            var bookingId = Int(doc.RootElement, "id");
            if (bookingId is null)
            {
                return new ChannelBookingPushResult(false, ErrorCode: "NoBookingId",
                    ErrorMessage: "OwnerRez did not return a booking id.");
            }

            return new ChannelBookingPushResult(true,
                ExternalBookingId: bookingId.Value.ToString(CultureInfo.InvariantCulture));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, nameof(PushBookingAsync), ex);
            return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                ErrorMessage: "OwnerRez create-booking request failed.");
        }
    }

    public async Task<IReadOnlyList<ChannelBookingUpdate>> PullBookingUpdatesAsync(
        ChannelCredentials credentials,
        DateTime changedSinceUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (!EnsureCredentials(credentials, nameof(PullBookingUpdatesAsync)))
        {
            return [];
        }

        var since = changedSinceUtc.ToUniversalTime()
            .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        return await ReadAllPagesAsync(
                credentials,
                $"/v2/bookings?since_utc={Uri.EscapeDataString(since)}",
                ParseBookingUpdate,
                ct)
            .ConfigureAwait(false);
    }

    // ── Listing content ──────────────────────────────────────────────────────

    private sealed record OwnerRezProperty(
        int Id,
        string Title,
        string? Currency,
        int? Bedrooms,
        decimal? Bathrooms,
        int? SquareFootage,
        double? Latitude,
        double? Longitude,
        string PropertyType,
        ChannelAddress? Address);

    private sealed record OwnerRezListingContent(
        int PropertyId,
        string? Headline,
        string? Description,
        long? NightlyCents,
        int? Bedrooms,
        decimal? Bathrooms,
        IReadOnlyList<string> AmenityCodes,
        IReadOnlyList<ChannelPhoto> Photos);

    private static OwnerRezProperty? ParseProperty(JsonElement item)
    {
        var id = Int(item, "id");
        if (id is null or <= 0)
        {
            return null;
        }

        var title = FirstNonEmpty(Str(item, "name"), Str(item, "external_name"))
            ?? id.Value.ToString(CultureInfo.InvariantCulture);

        ChannelAddress? address = null;
        if (item.TryGetProperty("address", out var addr) && addr.ValueKind == JsonValueKind.Object)
        {
            address = new ChannelAddress(
                Line1: FirstNonEmpty(Str(addr, "street1"), Str(addr, "street2")),
                City: Str(addr, "city"),
                State: FirstNonEmpty(Str(addr, "state"), Str(addr, "province")),
                PostalCode: Str(addr, "postal_code"),
                Country: Str(addr, "country"));
        }

        // `bathrooms` counts full and half rooms equally, so prefer the
        // half-weighted total Lagedra stores.
        var bathrooms = SumBathrooms(Int(item, "bathrooms_full"), Int(item, "bathrooms_half"))
            ?? Dec(item, "bathrooms");

        return new OwnerRezProperty(
            Id: id.Value,
            Title: title,
            Currency: Str(item, "currency_code"),
            Bedrooms: Int(item, "bedrooms"),
            Bathrooms: bathrooms,
            SquareFootage: ToSquareFeet(Int(item, "living_area"), Str(item, "living_area_type")),
            Latitude: Dbl(item, "latitude"),
            Longitude: Dbl(item, "longitude"),
            PropertyType: MapPropertyType(Str(item, "property_type")),
            Address: address);
    }

    private static OwnerRezListingContent? ParseListingContent(JsonElement item)
    {
        var propertyId = Int(item, "property_id");
        if (propertyId is null or <= 0)
        {
            return null;
        }

        string? headline = null;
        string? description = null;
        if (item.TryGetProperty("descriptions", out var d) && d.ValueKind == JsonValueKind.Object)
        {
            headline = Str(d, "headline");
            description = FirstNonEmpty(
                Str(d, "description"),
                Str(d, "short_description"),
                Str(d, "accommodations_summary"));
        }

        var nightly = Dec(item, "nightly_rate_min") ?? Dec(item, "nightly_rate_max");
        long? nightlyCents = nightly is > 0
            ? (long)Math.Round(nightly.Value * 100m, MidpointRounding.AwayFromZero)
            : null;

        var bathrooms = SumBathrooms(Int(item, "bathroom_full_count"), Int(item, "bathroom_half_count"))
            ?? Dec(item, "bathroom_count");

        return new OwnerRezListingContent(
            PropertyId: propertyId.Value,
            Headline: headline,
            Description: description,
            NightlyCents: nightlyCents,
            Bedrooms: Int(item, "bedroom_count"),
            Bathrooms: bathrooms,
            AmenityCodes: ParseAmenityCodes(item),
            Photos: ParsePhotos(item, propertyId.Value));
    }

    private static ChannelListingSnapshot BuildSnapshot(
        OwnerRezProperty property,
        OwnerRezListingContent? content)
    {
        var nightlyCents = content?.NightlyCents;
        var monthlyCents = nightlyCents.HasValue ? nightlyCents.Value * 30 : (long?)null;

        return new ChannelListingSnapshot(
            ExternalListingId: property.Id.ToString(CultureInfo.InvariantCulture),
            Title: FirstNonEmpty(property.Title, content?.Headline) ?? property.Title,
            Description: content?.Description,
            MonthlyRentCents: monthlyCents,
            NightlyRateCents: nightlyCents,
            Currency: property.Currency ?? "USD",
            MinStayNights: null,
            MaxStayNights: null,
            Bedrooms: property.Bedrooms ?? content?.Bedrooms,
            Bathrooms: property.Bathrooms ?? content?.Bathrooms,
            SquareFootage: property.SquareFootage,
            DepositCents: null,
            Latitude: property.Latitude,
            Longitude: property.Longitude,
            PropertyType: property.PropertyType,
            Address: property.Address,
            AmenityCodes: content?.AmenityCodes ?? [],
            Photos: content?.Photos ?? []);
    }

    private static List<ChannelPhoto> ParsePhotos(JsonElement item, int propertyId)
    {
        if (!item.TryGetProperty("photos", out var photos) || photos.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<ChannelPhoto>();
        var index = 0;
        foreach (var photo in photos.EnumerateArray())
        {
            var urlText = FirstNonEmpty(
                Str(photo, "original_url"),
                Str(photo, "large_url"),
                Str(photo, "cropped_url"));
            if (urlText is null || !Uri.TryCreate(urlText, UriKind.Absolute, out var uri))
            {
                continue;
            }

            // ImageFileModel carries no id, so derive a stable one from position.
            result.Add(new ChannelPhoto(
                $"{propertyId}-{index.ToString(CultureInfo.InvariantCulture)}",
                uri,
                Str(photo, "caption")));
            index++;
        }

        return result;
    }

    private static List<string> ParseAmenityCodes(JsonElement item)
    {
        var codes = new List<string>();

        if (item.TryGetProperty("amenity_categories", out var categories)
            && categories.ValueKind == JsonValueKind.Array)
        {
            foreach (var category in categories.EnumerateArray())
            {
                if (!category.TryGetProperty("amenities", out var amenities)
                    || amenities.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var amenity in amenities.EnumerateArray())
                {
                    AddAmenity(codes, amenity);
                }
            }
        }

        if (item.TryGetProperty("amenity_call_outs", out var callOuts)
            && callOuts.ValueKind == JsonValueKind.Array)
        {
            foreach (var callOut in callOuts.EnumerateArray())
            {
                AddAmenity(codes, callOut);
            }
        }

        return codes;
    }

    private static void AddAmenity(List<string> codes, JsonElement amenity)
    {
        var name = FirstNonEmpty(Str(amenity, "title"), Str(amenity, "text"));
        if (name is not null && !codes.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            codes.Add(name);
        }
    }

    // ── Availability ─────────────────────────────────────────────────────────

    /// <summary>
    /// Collects every night blocked by an active OwnerRez booking (or block)
    /// overlapping the window. v2 exposes no calendar feed, so availability is
    /// derived from the bookings list.
    /// </summary>
    private async Task<HashSet<DateOnly>> FetchBookedNightsAsync(
        ChannelCredentials credentials,
        int propertyId,
        DateOnly start,
        DateOnly end,
        CancellationToken ct)
    {
        var path =
            $"/v2/bookings?property_ids={propertyId}"
            + $"&from={Date(start)}&to={Date(end)}&status=active";

        var stays = await ReadAllPagesAsync(credentials, path, ParseStay, ct).ConfigureAwait(false);

        var booked = new HashSet<DateOnly>();
        foreach (var stay in stays)
        {
            // Departure day is a turnover day — the night itself is not booked.
            for (var night = stay.Arrival; night < stay.Departure; night = night.AddDays(1))
            {
                if (night >= start && night <= end)
                {
                    booked.Add(night);
                }
            }
        }

        return booked;
    }

    private sealed record OwnerRezStay(DateOnly Arrival, DateOnly Departure);

    private static OwnerRezStay? ParseStay(JsonElement item)
    {
        var arrival = ParseDate(Str(item, "arrival"));
        var departure = ParseDate(Str(item, "departure"));
        return arrival.HasValue && departure.HasValue && departure.Value > arrival.Value
            ? new OwnerRezStay(arrival.Value, departure.Value)
            : null;
    }

    private static List<ChannelDateBlock> CollapseToBlocks(Dictionary<DateOnly, bool> days)
    {
        if (days.Count == 0)
        {
            return [];
        }

        var ordered = days.OrderBy(kv => kv.Key).ToList();
        var blocks = new List<ChannelDateBlock>();
        var blockStart = ordered[0].Key;
        var blockAvailable = ordered[0].Value;
        var prev = ordered[0].Key;

        for (var i = 1; i < ordered.Count; i++)
        {
            var (date, available) = ordered[i];
            if (available == blockAvailable && date == prev.AddDays(1))
            {
                prev = date;
                continue;
            }

            blocks.Add(new ChannelDateBlock(blockStart, prev, blockAvailable));
            blockStart = date;
            blockAvailable = available;
            prev = date;
        }

        blocks.Add(new ChannelDateBlock(blockStart, prev, blockAvailable));
        return blocks;
    }

    // ── Booking push helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Finds the host's existing OwnerRez guest by email, creating one when the
    /// traveler is new. <c>POST /v2/bookings</c> requires a <c>guest_id</c>.
    /// </summary>
    private async Task<int?> ResolveGuestIdAsync(
        ChannelCredentials credentials,
        ChannelGuest guest,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(guest.Email))
        {
            var path = $"/v2/guests?q={Uri.EscapeDataString(guest.Email)}&limit={_settings.PageSize}";
            using var search = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
            if (search is not null
                && search.RootElement.TryGetProperty("items", out var items)
                && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var candidate in items.EnumerateArray())
                {
                    if (HasEmail(candidate, guest.Email) && Int(candidate, "id") is { } existingId)
                    {
                        return existingId;
                    }
                }
            }
        }

        var body = new Dictionary<string, object?>
        {
            ["first_name"] = guest.FirstName,
            ["last_name"] = guest.LastName,
        };

        if (!string.IsNullOrWhiteSpace(guest.Email))
        {
            body["email_addresses"] = new[]
            {
                new Dictionary<string, object?> { ["address"] = guest.Email, ["is_default"] = true },
            };
        }

        if (!string.IsNullOrWhiteSpace(guest.Phone))
        {
            body["phones"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["number"] = guest.Phone,
                    ["type"] = "mobile",
                    ["is_default"] = true,
                },
            };
        }

        try
        {
            using var response = await SendAsync(
                    credentials, HttpMethod.Post, "/v2/guests", JsonContent.Create(body), ct)
                .ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, nameof(ResolveGuestIdAsync), (int)response.StatusCode, "/v2/guests");
                return null;
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            return Int(doc.RootElement, "id");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, nameof(ResolveGuestIdAsync), ex);
            return null;
        }
    }

    private static bool HasEmail(JsonElement guest, string email)
    {
        if (!guest.TryGetProperty("email_addresses", out var emails)
            || emails.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in emails.EnumerateArray())
        {
            if (string.Equals(Str(entry, "address"), email, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// v2 bookings cannot carry charges, so the amounts Lagedra already
    /// collected are summarised in the notes field for host reconciliation.
    /// </summary>
    private static string BuildBookingNotes(ChannelBookingPushRequest request)
    {
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency;
        var notes = new StringBuilder();
        notes.Append(CultureInfo.InvariantCulture, $"Lagedra booking {request.TrackingReference}");
        notes.Append(CultureInfo.InvariantCulture,
            $" — {request.Adults} adult(s), {request.Children} child(ren), {request.Pets} pet(s).");
        notes.Append(CultureInfo.InvariantCulture, $" Payment status: {request.PaymentStatus}.");

        if (request.OrderItems.Count > 0)
        {
            notes.Append(" Charges:");
            foreach (var item in request.OrderItems)
            {
                notes.Append(CultureInfo.InvariantCulture,
                    $" {item.Name} {currency} {Money(item.AmountCents)};");
            }

            notes.Append(CultureInfo.InvariantCulture,
                $" Total {currency} {Money(request.OrderItems.Sum(i => i.AmountCents))}.");
        }

        if (request.OwnerCommissionCents is { } commission)
        {
            notes.Append(CultureInfo.InvariantCulture,
                $" Owner commission {currency} {Money(commission)}.");
        }

        if (request.GuestServiceFeeCents is { } serviceFee)
        {
            notes.Append(CultureInfo.InvariantCulture,
                $" Guest service fee {currency} {Money(serviceFee)}.");
        }

        if (!string.IsNullOrWhiteSpace(request.Message))
        {
            notes.Append(CultureInfo.InvariantCulture, $" Guest message: {request.Message}");
        }

        return notes.ToString();
    }

    /// <summary>
    /// Maps one OwnerRez booking object to a status update, returning null for
    /// blocks (which are not reservations) and for anything without an id. Public
    /// because webhook deliveries carry the same booking shape as the polled list,
    /// and the two paths must agree on what counts as cancelled.
    /// </summary>
    public static ChannelBookingUpdate? ParseBookingUpdate(JsonElement item)
    {
        var id = Int(item, "id");
        if (id is null)
        {
            return null;
        }

        if (item.TryGetProperty("is_block", out var isBlock)
            && isBlock.ValueKind == JsonValueKind.True)
        {
            return null;
        }

        var changedAt = ParseUtc(Str(item, "updated_utc"))
            ?? ParseUtc(Str(item, "booked_utc"))
            ?? ParseUtc(Str(item, "created_utc"))
            ?? DateTime.UtcNow;

        return new ChannelBookingUpdate(
            id.Value.ToString(CultureInfo.InvariantCulture),
            NormalizeBookingStatus(Str(item, "status")),
            changedAt);
    }

    // ── HTTP / paging ────────────────────────────────────────────────────────

    /// <summary>
    /// Reads every page of a v2 list endpoint, following <c>next_page_url</c>
    /// when present and falling back to <c>limit</c>/<c>offset</c> paging.
    /// </summary>
    private async Task<List<T>> ReadAllPagesAsync<T>(
        ChannelCredentials credentials,
        string path,
        Func<JsonElement, T?> map,
        CancellationToken ct,
        bool reportFailure = false)
        where T : class
    {
        var results = new List<T>();
        string? nextPageUrl = null;
        var offset = 0;

        for (var page = 0; page < _settings.MaxPages; page++)
        {
            var requestUri = nextPageUrl
                ?? AppendQuery(path, $"limit={_settings.PageSize}&offset={offset}");

            using var doc = await GetJsonAsync(credentials, requestUri, ct, reportFailure)
                .ConfigureAwait(false);
            if (doc is null
                || !doc.RootElement.TryGetProperty("items", out var items)
                || items.ValueKind != JsonValueKind.Array)
            {
                break;
            }

            var pageCount = 0;
            foreach (var item in items.EnumerateArray())
            {
                pageCount++;
                if (map(item) is { } mapped)
                {
                    results.Add(mapped);
                }
            }

            // A present-but-null next_page_url means "no more pages", so it is
            // authoritative; offset paging is only a fallback for responses
            // that omit the field entirely.
            var pagesLinked = doc.RootElement.TryGetProperty("next_page_url", out var nextEl);
            nextPageUrl = pagesLinked && nextEl.ValueKind == JsonValueKind.String
                ? nextEl.GetString()
                : null;

            if (nextPageUrl is not null)
            {
                continue;
            }

            if (pagesLinked || pageCount < _settings.PageSize)
            {
                break;
            }

            offset += _settings.PageSize;
        }

        return results;
    }

    /// <param name="reportFailure">
    /// When true, a rejected request throws instead of yielding null, so a caller
    /// on a host-visible path (the content sync) can record why it failed rather
    /// than reporting "no listings found". Background paths leave it false and
    /// degrade quietly.
    /// </param>
    private async Task<JsonDocument?> GetJsonAsync(
        ChannelCredentials credentials,
        string path,
        CancellationToken ct,
        bool reportFailure = false)
    {
        // Built inside the try but thrown outside it, so the catch below cannot
        // mistake a rejection for a transport failure and wrap it twice.
        string? rejection = null;

        try
        {
            using var response = await SendAsync(credentials, HttpMethod.Get, path, null, ct)
                .ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                return string.IsNullOrWhiteSpace(payload) ? null : JsonDocument.Parse(payload);
            }

            LogHttpError(logger, "GET", (int)response.StatusCode, path);
            rejection = DescribeFailure(response, credentials);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, $"GET {path}", ex);

            if (!reportFailure || ct.IsCancellationRequested)
            {
                return null;
            }

            throw new HttpRequestException(
                "Lagedra could not reach OwnerRez. Try syncing again shortly.", ex);
        }

        if (reportFailure)
        {
            throw new HttpRequestException(rejection);
        }

        return null;
    }

    /// <summary>
    /// Turns a rejection into something a host can act on.
    ///
    /// 429 is OwnerRez's documented request-volume limit — 300 requests per IP per
    /// 5 minutes — which clears on its own, so the message says to wait.
    ///
    /// The other limit worth naming is the cap of two accounts per IP per 24 hours
    /// on personal access tokens. OwnerRez documents the cap but not the status code
    /// it is enforced with, so it is raised only as a possibility on a 403 (the
    /// bucket their docs use for address-level blocks) rather than asserted on a
    /// specific code we would only be guessing at.
    /// </summary>
    private static string DescribeFailure(HttpResponseMessage response, ChannelCredentials credentials)
    {
        var tokenExpired = response.Headers.WwwAuthenticate
            .Any(h => h.Parameter?.Contains("token_expired", StringComparison.OrdinalIgnoreCase) == true);
        var personalToken = !(credentials.Secret ?? string.Empty).StartsWith("at_", StringComparison.Ordinal);

        return (int)response.StatusCode switch
        {
            401 when tokenExpired =>
                "Your OwnerRez authorization has expired. Disconnect and connect OwnerRez again.",
            401 when personalToken =>
                "OwnerRez rejected these credentials. Check that the email matches your OwnerRez "
                + "sign-in and that the access token is still active, then reconnect.",
            401 =>
                "OwnerRez rejected Lagedra's access to this account. Disconnect and connect OwnerRez again.",
            403 when personalToken =>
                "OwnerRez denied access with these credentials. Check the token is still active "
                + "under Settings, Advanced Tools, Developer/API Settings in OwnerRez. OwnerRez "
                + "also limits access tokens to two accounts per day from one address, which can "
                + "cause this even when your token is valid — contact us if it keeps happening.",
            403 =>
                "OwnerRez denied access to this account. Check that Lagedra is still authorized in "
                + "OwnerRez under Settings, Advanced Tools, Developer/API Settings.",
            429 =>
                "OwnerRez is rate limiting Lagedra right now. The next scheduled sync will pick up "
                + "where this one stopped.",
            var status when status >= 500 =>
                $"OwnerRez is temporarily unavailable (HTTP {status}). Try syncing again shortly.",
            var status =>
                $"OwnerRez rejected the request (HTTP {status}).",
        };
    }

    private async Task<HttpResponseMessage> SendAsync(
        ChannelCredentials credentials,
        HttpMethod method,
        string path,
        HttpContent? content,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.RelativeOrAbsolute))
        {
            Content = content,
        };
        request.Headers.Authorization = Authorization(credentials);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        return await httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// OwnerRez token prefixes are part of its documented contract: OAuth access
    /// tokens (<c>at_</c>) are sent as bearer tokens, while personal access tokens
    /// (<c>pt_</c>) use HTTP Basic with the account email as the username. Both are
    /// live — which one a host has depends on whether an OAuth app was configured
    /// when they connected — so the prefix, not deployment config, picks the scheme.
    /// </summary>
    private static AuthenticationHeaderValue Authorization(ChannelCredentials credentials)
    {
        var secret = credentials.Secret ?? string.Empty;
        if (secret.StartsWith("at_", StringComparison.Ordinal))
        {
            return new AuthenticationHeaderValue("bearer", secret);
        }

        var user = string.IsNullOrWhiteSpace(credentials.Username) ? secret : credentials.Username;
        var password = string.IsNullOrWhiteSpace(credentials.Username) ? string.Empty : secret;

        return new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{password}")));
    }

    private static string AppendQuery(string path, string query)
        => $"{path}{(path.Contains('?', StringComparison.Ordinal) ? '&' : '?')}{query}";

    private static bool HasCredentials(ChannelCredentials credentials)
        => !string.IsNullOrWhiteSpace(credentials.Secret);

    private bool EnsureCredentials(ChannelCredentials credentials, string method)
    {
        if (HasCredentials(credentials))
        {
            return true;
        }

        LogMissingToken(logger, method);
        return false;
    }

    private static bool TryParsePropertyId(string? externalListingId, out int propertyId)
        => int.TryParse(externalListingId, NumberStyles.Integer, CultureInfo.InvariantCulture, out propertyId)
           && propertyId > 0;

    private static string? ExtractErrorMessage(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("messages", out var messages)
                && messages.ValueKind == JsonValueKind.Array)
            {
                var joined = string.Join(
                    " ",
                    messages.EnumerateArray()
                        .Select(m => m.GetString())
                        .Where(m => !string.IsNullOrWhiteSpace(m)));
                return string.IsNullOrWhiteSpace(joined) ? null : joined;
            }

            return Str(doc.RootElement, "message");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // ── Value parsers / mapping ──────────────────────────────────────────────

    private static string? Str(JsonElement el, string name)
        => el.ValueKind == JsonValueKind.Object
           && el.TryGetProperty(name, out var v)
           && v.ValueKind == JsonValueKind.String
           && v.GetString()?.Trim() is { Length: > 0 } s
            ? s
            : null;

    private static int? Int(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object
            || !el.TryGetProperty(name, out var v)
            || v.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out var i))
        {
            return i;
        }

        return int.TryParse(v.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? Dec(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object
            || !el.TryGetProperty(name, out var v)
            || v.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var d))
        {
            return d;
        }

        return decimal.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static double? Dbl(JsonElement el, string name)
    {
        if (el.ValueKind != JsonValueKind.Object
            || !el.TryGetProperty(name, out var v)
            || v.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d))
        {
            return d;
        }

        return double.TryParse(v.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static decimal? SumBathrooms(int? full, int? half)
        => full is null && half is null ? null : (full ?? 0) + ((half ?? 0) * 0.5m);

    private static int? ToSquareFeet(int? livingArea, string? unit)
    {
        if (livingArea is null or <= 0)
        {
            return null;
        }

        var isMetric = unit is not null
            && (unit.Contains('m', StringComparison.OrdinalIgnoreCase)
                && !unit.Contains("ft", StringComparison.OrdinalIgnoreCase));

        return isMetric
            ? (int)Math.Round(livingArea.Value * 10.7639m, MidpointRounding.AwayFromZero)
            : livingArea.Value;
    }

    private static string Date(DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Money(long cents)
        => (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);

    private static DateOnly? ParseDate(string? raw)
        => DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? DateOnly.FromDateTime(dt)
            : null;

    private static DateTime? ParseUtc(string? raw)
        => DateTime.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)
            ? dt
            : null;

    /// <summary>Maps the v2 <c>PropertyType</c> enum onto Lagedra's listing types.</summary>
    private static string MapPropertyType(string? propertyType) => (propertyType ?? string.Empty) switch
    {
        "apartment" or "corporate_apartment" or "serviced_apartment" or "hotel_apartment" => "apartment",
        "condo" => "condo",
        "townhome" => "townhouse",
        "studio" => "studio",
        "loft" => "loft",
        "villa" => "villa",
        "cottage" => "cottage",
        "cabin" or "log_cabin" => "cabin",
        "house" or "bungalow" or "farmhouse" or "country_house" or "holiday_home" or "manor_house"
            or "estate" or "mobile_home" or "tiny_house" or "house_boat" => "house",
        _ => "other",
    };

    private static string NormalizeBookingStatus(string? status) => (status ?? string.Empty).ToUpperInvariant() switch
    {
        "ACTIVE" => "confirmed",
        "CANCELED" or "CANCELLED" => "cancelled",
        "PENDING" => "pending",
        _ => "pending",
    };

    // ── Structured logging ───────────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[OwnerRez] {Method} skipped — connection is missing a personal access token")]
    private static partial void LogMissingToken(ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[OwnerRez] {Method} got HTTP {StatusCode} for {RequestUri}")]
    private static partial void LogHttpError(ILogger logger, string method, int statusCode, string requestUri);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[OwnerRez] {Operation} failed")]
    private static partial void LogRequestException(ILogger logger, string operation, Exception ex);
}
