using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Channels.Guesty;

/// <summary>
/// Guesty Open API implementation of <see cref="IChannelProvider"/>.
/// Per-connection credentials are the host's OAuth Client ID
/// (<see cref="ChannelCredentials.ExternalAccountId"/>) and Client Secret
/// (<see cref="ChannelCredentials.Secret"/>). Tokens are cached aggressively
/// because Guesty allows only five token issues per client per 24 hours.
/// </summary>
public sealed partial class GuestyChannelProvider(
    HttpClient httpClient,
    IOptions<GuestyChannelSettings> settings,
    IMemoryCache cache,
    ILogger<GuestyChannelProvider> logger) : IChannelProvider
{
    private static readonly string ListingFields = string.Join(' ',
    [
        "_id", "id", "title", "nickname", "publicDescription", "address",
        "pictures", "picture", "amenities", "bedrooms", "bathrooms",
        "accommodates", "prices", "terms", "propertyType", "type",
        "active", "listed", "isListed",
    ]);

    private readonly GuestyChannelSettings _settings = settings.Value;

    public string ProviderKey => "guesty";

    public async Task<IReadOnlyList<ChannelListingSnapshot>> PullListingsAsync(
        ChannelCredentials credentials,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (!HasCredentials(credentials))
        {
            LogMissingSecret(logger, nameof(PullListingsAsync));
            return [];
        }

        var snapshots = new List<ChannelListingSnapshot>();
        var skip = 0;
        var fields = Uri.EscapeDataString(ListingFields);

        while (true)
        {
            var path =
                $"/v1/listings?limit={_settings.PageSize}&skip={skip}&fields={fields}&sort=_id";
            using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
            if (doc is null)
            {
                break;
            }

            if (!TryGetResultsArray(doc.RootElement, out var items) || items.GetArrayLength() == 0)
            {
                break;
            }

            foreach (var item in items.EnumerateArray())
            {
                var snapshot = ParseListing(item);
                if (snapshot is not null)
                {
                    snapshots.Add(snapshot);
                }
            }

            if (items.GetArrayLength() < _settings.PageSize)
            {
                break;
            }

            skip += _settings.PageSize;
        }

        return snapshots;
    }

    public async Task<ChannelAvailabilityCalendar> PullAvailabilityAsync(
        ChannelCredentials credentials,
        string externalListingId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var empty = new ChannelAvailabilityCalendar(externalListingId, []);
        if (!HasCredentials(credentials))
        {
            LogMissingSecret(logger, nameof(PullAvailabilityAsync));
            return empty;
        }

        var start = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var end = start.AddDays(Math.Max(30, _settings.AvailabilityLookaheadDays));
        var days = await FetchCalendarDaysAsync(credentials, externalListingId, start, end, ct)
            .ConfigureAwait(false);
        return new ChannelAvailabilityCalendar(externalListingId, CollapseToBlocks(days));
    }

    public async Task<ChannelAvailabilityResult> CheckAvailabilityAsync(
        ChannelCredentials credentials,
        ChannelAvailabilityQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(query);
        if (!HasCredentials(credentials))
        {
            LogMissingSecret(logger, nameof(CheckAvailabilityAsync));
            return new ChannelAvailabilityResult(false, "NotConfigured");
        }

        if (query.CheckOut <= query.CheckIn)
        {
            return new ChannelAvailabilityResult(false, "InvalidDates");
        }

        // Prefer Guesty's availability search for a single listing (respects occupancy).
        var occupancy = Math.Max(1, query.Adults + query.Children);
        var availableFilter = Uri.EscapeDataString(JsonSerializer.Serialize(new
        {
            checkIn = query.CheckIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            checkOut = query.CheckOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            minOccupancy = occupancy,
        }));
        var path =
            $"/v1/listings?ids={Uri.EscapeDataString(query.ExternalListingId)}" +
            $"&available={availableFilter}&fields=_id&limit=1";

        using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
        if (doc is not null && TryGetResultsArray(doc.RootElement, out var results))
        {
            foreach (var item in results.EnumerateArray())
            {
                var id = ReadId(item);
                if (string.Equals(id, query.ExternalListingId, StringComparison.OrdinalIgnoreCase))
                {
                    return new ChannelAvailabilityResult(true);
                }
            }

            return new ChannelAvailabilityResult(false, "Unavailable");
        }

        // Fallback: night-based calendar scan (checkout day excluded).
        var lastNight = query.CheckOut.AddDays(-1);
        var days = await FetchCalendarDaysAsync(
                credentials, query.ExternalListingId, query.CheckIn, lastNight, ct)
            .ConfigureAwait(false);

        if (days.Count == 0)
        {
            return new ChannelAvailabilityResult(false, "NoCalendar");
        }

        for (var d = query.CheckIn; d <= lastNight; d = d.AddDays(1))
        {
            if (!days.TryGetValue(d, out var available) || !available)
            {
                return new ChannelAvailabilityResult(false, "Unavailable");
            }
        }

        return new ChannelAvailabilityResult(true);
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
                ErrorMessage: "Guesty Client ID and Client Secret are not configured on this connection.");
        }

        if (string.IsNullOrWhiteSpace(request.ExternalListingId))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "InvalidListingId",
                ErrorMessage: "Guesty listing id is required.");
        }

        var totalCents = request.OrderItems.Sum(i => i.AmountCents);
        var cleaningCents = SumByType(request.OrderItems, "cleaning", "CLEANING");
        var depositCents = SumByType(request.OrderItems, "deposit", "DEPOSIT", "security");
        var accommodationCents = Math.Max(0, totalCents - cleaningCents - depositCents);

        var adults = Math.Max(1, request.Adults);
        var guestsCount = Math.Max(1, adults + request.Children);
        var phones = string.IsNullOrWhiteSpace(request.Guest.Phone)
            ? Array.Empty<string>()
            : new[] { request.Guest.Phone.Trim() };

        var body = new Dictionary<string, object?>
        {
            ["listingId"] = request.ExternalListingId,
            ["checkInDateLocalized"] = request.CheckIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["checkOutDateLocalized"] = request.CheckOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["source"] = string.IsNullOrWhiteSpace(_settings.ReservationSource)
                ? "manual"
                : _settings.ReservationSource,
            ["status"] = "confirmed",
            ["guestsCount"] = guestsCount,
            ["guest"] = new Dictionary<string, object?>
            {
                ["firstName"] = request.Guest.FirstName,
                ["lastName"] = request.Guest.LastName,
                ["email"] = request.Guest.Email,
                ["phones"] = phones,
            },
            ["numberOfGuests"] = new Dictionary<string, object?>
            {
                ["numberOfAdults"] = adults,
                ["numberOfChildren"] = Math.Max(0, request.Children),
                ["numberOfInfants"] = 0,
                ["numberOfPets"] = Math.Max(0, request.Pets),
            },
            ["accommodationFare"] = Money(accommodationCents),
            ["applyPromotions"] = false,
            ["origin"] = "Lagedra",
            ["originId"] = Truncate(request.TrackingReference, 50),
            ["confirmationCode"] = Truncate(request.TrackingReference, 50),
        };

        if (cleaningCents > 0)
        {
            body["cleaningFee"] = Money(cleaningCents);
        }

        try
        {
            using var response = await SendAuthorizedAsync(
                    credentials,
                    HttpMethod.Post,
                    "/v1/reservations-v3",
                    JsonContent.Create(body),
                    ct)
                .ConfigureAwait(false);

            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, nameof(PushBookingAsync), (int)response.StatusCode, "/v1/reservations-v3");
                return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                    ErrorMessage: ExtractErrorMessage(payload) ?? "Guesty rejected the reservation.");
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            var externalId = ReadReservationId(doc.RootElement);
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return new ChannelBookingPushResult(false, ErrorCode: "NoBookingId",
                    ErrorMessage: "Guesty did not return a reservation id.");
            }

            return new ChannelBookingPushResult(true, ExternalBookingId: externalId);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, nameof(PushBookingAsync), ex);
            return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                ErrorMessage: "Guesty create-reservation request failed.");
        }
    }

    public async Task<IReadOnlyList<ChannelBookingUpdate>> PullBookingUpdatesAsync(
        ChannelCredentials credentials,
        DateTime changedSinceUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (!HasCredentials(credentials))
        {
            LogMissingSecret(logger, nameof(PullBookingUpdatesAsync));
            return [];
        }

        var updates = new List<ChannelBookingUpdate>();
        var skip = 0;
        var fields = Uri.EscapeDataString("_id id status lastUpdatedAt confirmedAt canceledAt");

        while (true)
        {
            var path =
                $"/v1/reservations?limit={_settings.PageSize}&skip={skip}&fields={fields}&sort=-lastUpdatedAt";
            using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
            if (doc is null)
            {
                break;
            }

            if (!TryGetResultsArray(doc.RootElement, out var items) || items.GetArrayLength() == 0)
            {
                break;
            }

            var oldestOnPage = DateTime.MaxValue;
            foreach (var item in items.EnumerateArray())
            {
                var id = ReadId(item);
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var changedAt = ParseUtc(Str(item, "lastUpdatedAt"))
                    ?? ParseUtc(Str(item, "confirmedAt"))
                    ?? ParseUtc(Str(item, "canceledAt"))
                    ?? DateTime.UtcNow;

                if (changedAt < oldestOnPage)
                {
                    oldestOnPage = changedAt;
                }

                if (changedAt < changedSinceUtc)
                {
                    continue;
                }

                var status = Str(item, "status");
                updates.Add(new ChannelBookingUpdate(id, NormalizeBookingStatus(status), changedAt));
            }

            // Sorted newest-first; stop once the whole page is older than the cursor.
            if (oldestOnPage < changedSinceUtc || items.GetArrayLength() < _settings.PageSize)
            {
                break;
            }

            skip += _settings.PageSize;
        }

        return updates;
    }

    // ── Calendar ─────────────────────────────────────────────────────────────

    private async Task<Dictionary<DateOnly, bool>> FetchCalendarDaysAsync(
        ChannelCredentials credentials,
        string externalListingId,
        DateOnly start,
        DateOnly end,
        CancellationToken ct)
    {
        var result = new Dictionary<DateOnly, bool>();
        // Guesty calendar windows stay reasonably sized for payload/latency.
        for (var windowStart = start; windowStart <= end; windowStart = windowStart.AddDays(90))
        {
            var windowEnd = windowStart.AddDays(89);
            if (windowEnd > end)
            {
                windowEnd = end;
            }

            var path =
                $"/v1/availability-pricing/api/calendar/listings/{Uri.EscapeDataString(externalListingId)}" +
                $"?startDate={windowStart:yyyy-MM-dd}&endDate={windowEnd:yyyy-MM-dd}&includeAllotment=true";
            using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
            if (doc is null || !TryGetCalendarDays(doc.RootElement, out var days))
            {
                continue;
            }

            foreach (var day in days.EnumerateArray())
            {
                var dateRaw = Str(day, "date");
                if (!DateOnly.TryParse(dateRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    continue;
                }

                result[date] = IsDayAvailable(day);
            }
        }

        return result;
    }

    private static bool IsDayAvailable(JsonElement day)
    {
        // Multi-unit: allotment > 0 wins over status.
        if (day.TryGetProperty("allotment", out var allotment) && allotment.ValueKind == JsonValueKind.Number)
        {
            return allotment.GetDecimal() > 0;
        }

        var status = Str(day, "status");
        return string.Equals(status, "available", StringComparison.OrdinalIgnoreCase);
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

    // ── Listing parsing ──────────────────────────────────────────────────────

    private static ChannelListingSnapshot? ParseListing(JsonElement item)
    {
        var externalId = ReadId(item);
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        // Skip inactive / unlisted when those flags are present.
        if (item.TryGetProperty("active", out var activeEl)
            && activeEl.ValueKind is JsonValueKind.True or JsonValueKind.False
            && !activeEl.GetBoolean())
        {
            return null;
        }

        if (item.TryGetProperty("listed", out var listedEl)
            && listedEl.ValueKind is JsonValueKind.True or JsonValueKind.False
            && !listedEl.GetBoolean())
        {
            return null;
        }

        if (item.TryGetProperty("isListed", out var isListedEl)
            && isListedEl.ValueKind is JsonValueKind.True or JsonValueKind.False
            && !isListedEl.GetBoolean())
        {
            return null;
        }

        var title = FirstNonEmpty(Str(item, "title"), Str(item, "nickname"), externalId)!;
        var description = ReadDescription(item);

        decimal? basePrice = null;
        string? currency = null;
        decimal? deposit = null;
        if (item.TryGetProperty("prices", out var prices) && prices.ValueKind == JsonValueKind.Object)
        {
            basePrice = Dec(prices, "basePrice") ?? Dec(prices, "nightly");
            currency = Str(prices, "currency");
            deposit = Dec(prices, "securityDepositFee") ?? Dec(prices, "securityDeposit");
        }

        long? nightlyCents = basePrice is > 0
            ? (long)Math.Round(basePrice.Value * 100m, MidpointRounding.AwayFromZero)
            : null;
        long? monthlyCents = nightlyCents.HasValue ? nightlyCents.Value * 30 : null;
        long? depositCents = deposit is > 0
            ? (long)Math.Round(deposit.Value * 100m, MidpointRounding.AwayFromZero)
            : null;

        int? minNights = null;
        int? maxNights = null;
        if (item.TryGetProperty("terms", out var terms) && terms.ValueKind == JsonValueKind.Object)
        {
            minNights = Int(terms, "minNights");
            maxNights = Int(terms, "maxNights");
        }

        ChannelAddress? address = null;
        double? lat = null;
        double? lng = null;
        if (item.TryGetProperty("address", out var addr) && addr.ValueKind == JsonValueKind.Object)
        {
            address = new ChannelAddress(
                Line1: FirstNonEmpty(Str(addr, "street"), Str(addr, "full")),
                City: Str(addr, "city"),
                State: Str(addr, "state"),
                PostalCode: Str(addr, "zipcode") ?? Str(addr, "zipCode"),
                Country: Str(addr, "country"));
            lat = Dbl(addr, "lat");
            lng = Dbl(addr, "lng");
        }

        return new ChannelListingSnapshot(
            ExternalListingId: externalId,
            Title: title,
            Description: description,
            MonthlyRentCents: monthlyCents,
            NightlyRateCents: nightlyCents,
            Currency: currency ?? "USD",
            MinStayNights: minNights,
            MaxStayNights: maxNights,
            Bedrooms: Int(item, "bedrooms"),
            Bathrooms: Dec(item, "bathrooms"),
            SquareFootage: null,
            DepositCents: depositCents,
            Latitude: lat,
            Longitude: lng,
            PropertyType: MapPropertyType(Str(item, "propertyType"), Str(item, "type")),
            Address: address,
            AmenityCodes: ParseAmenities(item),
            Photos: ParsePhotos(item));
    }

    private static string? ReadDescription(JsonElement item)
    {
        if (item.TryGetProperty("publicDescription", out var pub))
        {
            if (pub.ValueKind == JsonValueKind.String)
            {
                return pub.GetString()?.Trim();
            }

            if (pub.ValueKind == JsonValueKind.Object)
            {
                return FirstNonEmpty(
                    Str(pub, "summary"),
                    Str(pub, "space"),
                    Str(pub, "access"),
                    Str(pub, "neighborhood"),
                    Str(pub, "notes"),
                    Str(pub, "transit"),
                    Str(pub, "interaction"));
            }
        }

        return Str(item, "description");
    }

    private static List<ChannelPhoto> ParsePhotos(JsonElement item)
    {
        JsonElement pictures = default;
        var found = false;
        if (item.TryGetProperty("pictures", out pictures) && pictures.ValueKind == JsonValueKind.Array)
        {
            found = true;
        }
        else if (item.TryGetProperty("picture", out var picture) && picture.ValueKind == JsonValueKind.Object)
        {
            // Single primary picture object.
            var urlText = FirstNonEmpty(Str(picture, "original"), Str(picture, "regular"), Str(picture, "thumbnail"));
            if (urlText is not null && Uri.TryCreate(urlText, UriKind.Absolute, out var uri))
            {
                var id = FirstNonEmpty(Str(picture, "_id"), Str(picture, "id")) ?? Guid.NewGuid().ToString("n");
                return [new ChannelPhoto(id, uri, Str(picture, "caption"))];
            }

            return [];
        }

        if (!found)
        {
            return [];
        }

        var photos = new List<ChannelPhoto>();
        foreach (var img in pictures.EnumerateArray())
        {
            var urlText = FirstNonEmpty(
                Str(img, "original"),
                Str(img, "regular"),
                Str(img, "large"),
                Str(img, "thumbnail"),
                Str(img, "url"));
            if (urlText is null || !Uri.TryCreate(urlText, UriKind.Absolute, out var uri))
            {
                continue;
            }

            var photoId = FirstNonEmpty(Str(img, "_id"), Str(img, "id")) ?? Guid.NewGuid().ToString("n");
            photos.Add(new ChannelPhoto(photoId, uri, Str(img, "caption")));
        }

        return photos;
    }

    private static List<string> ParseAmenities(JsonElement item)
    {
        if (!item.TryGetProperty("amenities", out var amenities))
        {
            return [];
        }

        var codes = new List<string>();
        if (amenities.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in amenities.EnumerateArray())
            {
                if (a.ValueKind == JsonValueKind.String)
                {
                    var name = a.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        codes.Add(name!);
                    }
                }
                else if (a.ValueKind == JsonValueKind.Object)
                {
                    var name = FirstNonEmpty(Str(a, "name"), Str(a, "amenity"), Str(a, "key"));
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        codes.Add(name!);
                    }
                }
            }
        }

        return codes;
    }

    private static string MapPropertyType(string? propertyType, string? listingType)
    {
        var raw = FirstNonEmpty(propertyType, listingType)?.ToUpperInvariant() ?? string.Empty;
        if (raw.Contains("APARTMENT", StringComparison.Ordinal)) return "apartment";
        if (raw.Contains("CONDO", StringComparison.Ordinal)) return "condo";
        if (raw.Contains("TOWNHOUSE", StringComparison.Ordinal) || raw.Contains("TOWNHOME", StringComparison.Ordinal))
            return "townhouse";
        if (raw.Contains("VILLA", StringComparison.Ordinal)) return "villa";
        if (raw.Contains("CABIN", StringComparison.Ordinal)) return "cabin";
        if (raw.Contains("COTTAGE", StringComparison.Ordinal)) return "cottage";
        if (raw.Contains("STUDIO", StringComparison.Ordinal)) return "studio";
        if (raw.Contains("LOFT", StringComparison.Ordinal)) return "loft";
        if (raw.Contains("HOUSE", StringComparison.Ordinal) || raw.Contains("HOME", StringComparison.Ordinal))
            return "house";
        if (raw.Contains("ROOM", StringComparison.Ordinal)) return "room";
        return "other";
    }

    // ── HTTP / auth ──────────────────────────────────────────────────────────

    private async Task<JsonDocument?> GetJsonAsync(
        ChannelCredentials credentials,
        string path,
        CancellationToken ct)
    {
        try
        {
            using var response = await SendAuthorizedAsync(credentials, HttpMethod.Get, path, null, ct)
                .ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, "GET", (int)response.StatusCode, path);
                return null;
            }

            if (string.IsNullOrWhiteSpace(payload))
            {
                return null;
            }

            return JsonDocument.Parse(payload);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, $"GET {path}", ex);
            return null;
        }
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        ChannelCredentials credentials,
        HttpMethod method,
        string path,
        HttpContent? content,
        CancellationToken ct)
    {
        byte[]? bodyBytes = null;
        MediaTypeHeaderValue? contentType = null;
        if (content is not null)
        {
            bodyBytes = await content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            contentType = content.Headers.ContentType;
            content.Dispose();
        }

        var token = await GetAccessTokenAsync(credentials, forceRefresh: false, ct).ConfigureAwait(false);
        var response = await SendWithTokenAsync(method, path, bodyBytes, contentType, token, ct)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            response.Dispose();
            token = await GetAccessTokenAsync(credentials, forceRefresh: true, ct).ConfigureAwait(false);
            response = await SendWithTokenAsync(method, path, bodyBytes, contentType, token, ct)
                .ConfigureAwait(false);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(
        HttpMethod method,
        string path,
        byte[]? bodyBytes,
        MediaTypeHeaderValue? contentType,
        string token,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.RelativeOrAbsolute));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        if (bodyBytes is not null)
        {
            var body = new ByteArrayContent(bodyBytes);
            if (contentType is not null)
            {
                body.Headers.ContentType = contentType;
            }

            request.Content = body;
        }

        return await httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    private async Task<string> GetAccessTokenAsync(
        ChannelCredentials credentials,
        bool forceRefresh,
        CancellationToken ct)
    {
        var cacheKey = $"guesty:token:{credentials.ExternalAccountId}";
        if (!forceRefresh
            && cache.TryGetValue(cacheKey, out string? cached)
            && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        if (string.IsNullOrWhiteSpace(credentials.Secret))
        {
            throw new InvalidOperationException("Guesty client secret is required.");
        }

        // Prefer form-urlencoded (official auth guide). Guesty allows only 5 tokens / 24h.
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = "open-api",
            ["client_id"] = credentials.ExternalAccountId,
            ["client_secret"] = credentials.Secret,
        });

        using var response = await httpClient
            .PostAsync(new Uri("/oauth2/token", UriKind.Relative), form, ct)
            .ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            LogHttpError(logger, "oauth2/token", (int)response.StatusCode, "/oauth2/token");
            throw new InvalidOperationException(
                ExtractErrorMessage(payload) ?? "Failed to obtain Guesty access token.");
        }

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var token = Str(root, "access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Guesty access token response was empty.");
        }

        // Tokens are typically 24h. Refresh ~1h early to stay under the 5/day quota.
        var expiresIn = root.TryGetProperty("expires_in", out var expEl) && expEl.TryGetInt32(out var seconds)
            ? seconds
            : 86_400;
        var ttl = TimeSpan.FromSeconds(Math.Max(300, expiresIn - 3600));
        cache.Set(cacheKey, token, ttl);
        return token;
    }

    // ── JSON helpers ─────────────────────────────────────────────────────────

    private static bool HasCredentials(ChannelCredentials credentials)
        => !string.IsNullOrWhiteSpace(credentials.ExternalAccountId)
           && !string.IsNullOrWhiteSpace(credentials.Secret);

    private static bool TryGetResultsArray(JsonElement root, out JsonElement array)
    {
        if (root.TryGetProperty("results", out array) && array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        if (root.TryGetProperty("result", out array) && array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        if (root.TryGetProperty("data", out array) && array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        array = default;
        return false;
    }

    private static bool TryGetCalendarDays(JsonElement root, out JsonElement array)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            array = root;
            return true;
        }

        if (root.TryGetProperty("data", out var data))
        {
            if (data.ValueKind == JsonValueKind.Array)
            {
                array = data;
                return true;
            }

            if (data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("days", out array)
                && array.ValueKind == JsonValueKind.Array)
            {
                return true;
            }

            if (data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("calendar", out array)
                && array.ValueKind == JsonValueKind.Array)
            {
                return true;
            }
        }

        if (root.TryGetProperty("days", out array) && array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        if (root.TryGetProperty("calendar", out array) && array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        array = default;
        return false;
    }

    private static string? ReadId(JsonElement item)
        => FirstNonEmpty(Str(item, "_id"), Str(item, "id"))
           ?? (item.TryGetProperty("_id", out var idEl) ? idEl.ToString() : null)
           ?? (item.TryGetProperty("id", out var id2) ? id2.ToString() : null);

    private static string? ReadReservationId(JsonElement root)
        => FirstNonEmpty(
            Str(root, "reservationId"),
            Str(root, "_id"),
            Str(root, "id"),
            root.TryGetProperty("reservation", out var res) && res.ValueKind == JsonValueKind.Object
                ? FirstNonEmpty(Str(res, "_id"), Str(res, "id"), Str(res, "reservationId"))
                : null);

    private static string? ExtractErrorMessage(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("message", out var message))
            {
                return message.GetString();
            }

            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString();
                }

                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var nested))
                {
                    return nested.GetString();
                }
            }
        }
        catch (JsonException)
        {
            // fall through
        }

        return null;
    }

    private static string? Str(JsonElement el, string name)
        => el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? (v.GetString()?.Trim() is { Length: > 0 } s ? s : null)
            : null;

    private static int? Int(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null)
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
        if (!el.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null)
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
        if (!el.TryGetProperty(name, out var v) || v.ValueKind == JsonValueKind.Null)
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

    private static decimal Money(long cents)
        => cents / 100m;

    private static long SumByType(IReadOnlyList<ChannelOrderItem> items, params string[] needles)
        => items
            .Where(i => needles.Any(n =>
                i.Type.Contains(n, StringComparison.OrdinalIgnoreCase)
                || i.Name.Contains(n, StringComparison.OrdinalIgnoreCase)))
            .Sum(i => i.AmountCents);

    private static DateTime? ParseUtc(string? raw)
        => DateTime.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)
            ? dt
            : null;

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
    }

    private static string NormalizeBookingStatus(string? status) => (status ?? string.Empty).ToUpperInvariant() switch
    {
        "CONFIRMED" => "confirmed",
        "CANCELED" or "CANCELLED" or "DECLINED" or "EXPIRED" or "CLOSED" => "cancelled",
        "RESERVED" or "INQUIRY" or "AWAITING_PAYMENT" => "pending",
        _ => "pending",
    };

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Guesty] {Method} skipped — connection is missing Client ID or Client Secret")]
    private static partial void LogMissingSecret(ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Guesty] {Method} got HTTP {StatusCode} for {RequestUri}")]
    private static partial void LogHttpError(ILogger logger, string method, int statusCode, string requestUri);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Guesty] {Operation} failed")]
    private static partial void LogRequestException(ILogger logger, string operation, Exception ex);
}
