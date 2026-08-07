using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Channels.Smoobu;

/// <summary>
/// Smoobu implementation of <see cref="IChannelProvider"/> against the Smoobu
/// REST API (login.smoobu.com). Per-connection credentials are the host's
/// Smoobu API key (<see cref="ChannelCredentials.ExternalAccountId"/>) and API
/// secret (<see cref="ChannelCredentials.Secret"/>); every request is signed
/// with HMAC-SHA256 per Smoobu's HMAC authentication scheme (the legacy
/// <c>Api-Key</c> header sunsets September 2026).
/// </summary>
public sealed partial class SmoobuChannelProvider(
    HttpClient httpClient,
    IOptions<SmoobuChannelSettings> settings,
    IMemoryCache cache,
    ILogger<SmoobuChannelProvider> logger) : IChannelProvider
{
    private static readonly TimeSpan CustomerIdCacheTtl = TimeSpan.FromHours(12);

    private readonly SmoobuChannelSettings _settings = settings.Value;

    public string ProviderKey => "smoobu";

    // ── Listings ─────────────────────────────────────────────────────────────

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

        using var index = await GetJsonAsync(credentials, "/api/apartments", ct).ConfigureAwait(false);
        if (index is null
            || !index.RootElement.TryGetProperty("apartments", out var apartments)
            || apartments.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var snapshots = new List<ChannelListingSnapshot>();
        foreach (var apartment in apartments.EnumerateArray())
        {
            if (!apartment.TryGetProperty("id", out var idEl))
            {
                continue;
            }

            var externalId = idEl.ToString();
            if (string.IsNullOrWhiteSpace(externalId))
            {
                continue;
            }

            var title = Str(apartment, "name") ?? externalId;

            // The index only carries id + name; details (location, rooms,
            // amenities, prices) come from the per-apartment endpoint. A failed
            // detail call still yields a minimal snapshot so the sync proceeds.
            using var detail = await GetJsonAsync(
                    credentials, $"/api/apartments/{Uri.EscapeDataString(externalId)}", ct)
                .ConfigureAwait(false);

            snapshots.Add(detail is null
                ? new ChannelListingSnapshot(externalId, title)
                : ParseApartmentDetail(externalId, title, detail.RootElement));
        }

        return snapshots;
    }

    private static ChannelListingSnapshot ParseApartmentDetail(
        string externalId,
        string title,
        JsonElement root)
    {
        ChannelAddress? address = null;
        double? latitude = null;
        double? longitude = null;
        if (root.TryGetProperty("location", out var location)
            && location.ValueKind == JsonValueKind.Object)
        {
            address = new ChannelAddress(
                Line1: Str(location, "street"),
                City: Str(location, "city"),
                PostalCode: Str(location, "zip"),
                Country: Str(location, "country"));
            latitude = Dbl(location, "latitude");
            longitude = Dbl(location, "longitude");
        }

        int? bedrooms = null;
        decimal? bathrooms = null;
        if (root.TryGetProperty("rooms", out var rooms) && rooms.ValueKind == JsonValueKind.Object)
        {
            bedrooms = Int(rooms, "bedrooms");
            bathrooms = Dec(rooms, "bathrooms");
        }

        long? nightlyCents = null;
        if (root.TryGetProperty("price", out var price) && price.ValueKind == JsonValueKind.Object)
        {
            var minimal = Dec(price, "minimal");
            nightlyCents = minimal is > 0
                ? (long)Math.Round(minimal.Value * 100m, MidpointRounding.AwayFromZero)
                : null;
        }

        // Docs show "equipments" in the example payload but "amenities" in the
        // response table — accept either.
        var amenities = ParseStringArray(root, "equipments") ?? ParseStringArray(root, "amenities");

        string? typeName = null;
        if (root.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.Object)
        {
            typeName = Str(type, "name");
        }

        return new ChannelListingSnapshot(
            ExternalListingId: externalId,
            Title: title,
            MonthlyRentCents: nightlyCents.HasValue ? nightlyCents.Value * 30 : null,
            NightlyRateCents: nightlyCents,
            Currency: Str(root, "currency") ?? "USD",
            Bedrooms: bedrooms,
            Bathrooms: bathrooms,
            Latitude: latitude,
            Longitude: longitude,
            PropertyType: MapPropertyType(typeName),
            Address: address,
            AmenityCodes: amenities);
    }

    private static List<string>? ParseStringArray(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var values = new List<string>();
        foreach (var item in arr.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String
                && item.GetString() is { Length: > 0 } value)
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static string MapPropertyType(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return "other";
        }

        if (typeName.Contains("apartment", StringComparison.OrdinalIgnoreCase))
        {
            return "apartment";
        }

        if (typeName.Contains("house", StringComparison.OrdinalIgnoreCase)
            || typeName.Contains("home", StringComparison.OrdinalIgnoreCase))
        {
            return "house";
        }

        if (typeName.Contains("villa", StringComparison.OrdinalIgnoreCase))
        {
            return "villa";
        }

        if (typeName.Contains("studio", StringComparison.OrdinalIgnoreCase))
        {
            return "studio";
        }

        return "other";
    }

    // ── Availability ─────────────────────────────────────────────────────────

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
        var days = await FetchRateDaysAsync(credentials, externalListingId, start, end, ct)
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

        if (!long.TryParse(query.ExternalListingId, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var apartmentId))
        {
            return new ChannelAvailabilityResult(false, "InvalidListingId");
        }

        var customerId = await GetCustomerIdAsync(credentials, ct).ConfigureAwait(false);
        if (customerId is null)
        {
            return new ChannelAvailabilityResult(false, "NotConfigured");
        }

        var body = new Dictionary<string, object?>
        {
            ["arrivalDate"] = Iso(query.CheckIn),
            ["departureDate"] = Iso(query.CheckOut),
            ["apartments"] = new[] { apartmentId },
            ["customerId"] = customerId.Value,
            ["guests"] = Math.Max(1, query.Adults + query.Children),
        };

        using var doc = await PostJsonAsync(
                credentials, "/booking/checkApartmentAvailability", body, ct)
            .ConfigureAwait(false);
        if (doc is null)
        {
            return new ChannelAvailabilityResult(false, "RequestFailed");
        }

        if (doc.RootElement.TryGetProperty("availableApartments", out var available)
            && available.ValueKind == JsonValueKind.Array
            && available.EnumerateArray().Any(a => a.ToString() == query.ExternalListingId))
        {
            return new ChannelAvailabilityResult(true);
        }

        return new ChannelAvailabilityResult(false, "Unavailable");
    }

    private async Task<Dictionary<DateOnly, bool>> FetchRateDaysAsync(
        ChannelCredentials credentials,
        string externalListingId,
        DateOnly start,
        DateOnly end,
        CancellationToken ct)
    {
        var result = new Dictionary<DateOnly, bool>();
        // Pre-encode the brackets so the signed query string matches the wire
        // format byte-for-byte regardless of HttpClient's escaping rules.
        var path = $"/api/rates?apartments%5B%5D={Uri.EscapeDataString(externalListingId)}"
                   + $"&start_date={Iso(start)}&end_date={Iso(end)}";
        using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
        if (doc is null
            || !doc.RootElement.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Object
            || !data.TryGetProperty(externalListingId, out var days)
            || days.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var day in days.EnumerateObject())
        {
            if (!DateOnly.TryParse(day.Name, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
            {
                continue;
            }

            var available = day.Value.TryGetProperty("available", out var availEl)
                            && availEl.ValueKind == JsonValueKind.Number
                            && availEl.GetInt32() > 0;
            result[date] = available;
        }

        return result;
    }

    private async Task<long?> GetCustomerIdAsync(
        ChannelCredentials credentials,
        CancellationToken ct)
    {
        var cacheKey = $"smoobu:customerId:{credentials.ExternalAccountId}";
        if (cache.TryGetValue(cacheKey, out long cached) && cached > 0)
        {
            return cached;
        }

        using var doc = await GetJsonAsync(credentials, "/api/me", ct).ConfigureAwait(false);
        if (doc is null
            || !doc.RootElement.TryGetProperty("id", out var idEl)
            || !idEl.TryGetInt64(out var id))
        {
            return null;
        }

        cache.Set(cacheKey, id, CustomerIdCacheTtl);
        return id;
    }

    // ── Booking push ─────────────────────────────────────────────────────────

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
                ErrorMessage: "Smoobu API credentials are not configured on this connection.");
        }

        if (!long.TryParse(request.ExternalListingId, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var apartmentId))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "InvalidListingId",
                ErrorMessage: "Smoobu apartment id must be numeric.");
        }

        var totalCents = request.OrderItems.Sum(i => i.AmountCents);
        var depositCents = SumByType(request.OrderItems, "deposit", "security");

        var body = new Dictionary<string, object?>
        {
            ["arrivalDate"] = Iso(request.CheckIn),
            ["departureDate"] = Iso(request.CheckOut),
            ["channelId"] = _settings.DefaultChannelId,
            ["apartmentId"] = apartmentId,
            ["firstName"] = request.Guest.FirstName,
            ["lastName"] = request.Guest.LastName,
            ["email"] = request.Guest.Email,
            ["phone"] = request.Guest.Phone,
            ["adults"] = Math.Max(1, request.Adults),
            ["children"] = request.Children,
            ["price"] = Money(totalCents),
            // priceStatus 1 = paid; Lagedra is merchant of record.
            ["priceStatus"] = 1,
            ["notice"] = string.IsNullOrWhiteSpace(request.Message)
                ? $"Lagedra booking {request.TrackingReference}"
                : request.Message,
        };

        if (depositCents > 0)
        {
            body["deposit"] = Money(depositCents);
            body["depositStatus"] = 1;
        }

        using var doc = await PostJsonAsync(credentials, "/api/reservations", body, ct)
            .ConfigureAwait(false);
        if (doc is null)
        {
            return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                ErrorMessage: "Smoobu rejected the reservation.");
        }

        var externalId = doc.RootElement.TryGetProperty("id", out var idEl)
            ? idEl.ToString()
            : null;
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "NoBookingId",
                ErrorMessage: "Smoobu did not return a reservation id.");
        }

        return new ChannelBookingPushResult(true, ExternalBookingId: externalId);
    }

    // ── Booking updates ──────────────────────────────────────────────────────

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

        var since = changedSinceUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var updates = new List<ChannelBookingUpdate>();

        for (var page = 1; page <= Math.Max(1, _settings.MaxPages); page++)
        {
            var path = $"/api/reservations?modifiedFrom={since}&showCancellation=true"
                       + $"&pageSize={_settings.PageSize}&page={page}";
            using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
            if (doc is null
                || !doc.RootElement.TryGetProperty("bookings", out var bookings)
                || bookings.ValueKind != JsonValueKind.Array)
            {
                break;
            }

            foreach (var booking in bookings.EnumerateArray())
            {
                var update = ParseBookingUpdate(booking);
                if (update is not null)
                {
                    updates.Add(update);
                }
            }

            var pageCount = Int(doc.RootElement, "page_count") ?? 1;
            if (page >= pageCount)
            {
                break;
            }
        }

        return updates;
    }

    private static ChannelBookingUpdate? ParseBookingUpdate(JsonElement booking)
    {
        if (!booking.TryGetProperty("id", out var idEl))
        {
            return null;
        }

        var id = idEl.ToString();
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        // Blocked-period pseudo-bookings are calendar noise, not guest bookings.
        if (booking.TryGetProperty("is-blocked-booking", out var blocked)
            && blocked.ValueKind == JsonValueKind.True)
        {
            return null;
        }

        var type = Str(booking, "type");
        var status = type is not null
                     && type.Contains("cancel", StringComparison.OrdinalIgnoreCase)
            ? "cancelled"
            : "confirmed";

        var changedAt = ParseUtc(Str(booking, "modifiedAt"))
                        ?? ParseUtc(Str(booking, "created-at"))
                        ?? DateTime.UtcNow;

        return new ChannelBookingUpdate(id, status, changedAt);
    }

    // ── HTTP / HMAC signing ──────────────────────────────────────────────────

    private async Task<JsonDocument?> GetJsonAsync(
        ChannelCredentials credentials,
        string pathAndQuery,
        CancellationToken ct)
        => await SendSignedAsync(credentials, HttpMethod.Get, pathAndQuery, null, ct)
            .ConfigureAwait(false);

    private async Task<JsonDocument?> PostJsonAsync(
        ChannelCredentials credentials,
        string pathAndQuery,
        Dictionary<string, object?> body,
        CancellationToken ct)
        => await SendSignedAsync(credentials, HttpMethod.Post, pathAndQuery, body, ct)
            .ConfigureAwait(false);

    private async Task<JsonDocument?> SendSignedAsync(
        ChannelCredentials credentials,
        HttpMethod method,
        string pathAndQuery,
        Dictionary<string, object?>? body,
        CancellationToken ct)
    {
        var bodyBytes = body is null ? [] : JsonSerializer.SerializeToUtf8Bytes(body);

        try
        {
            using var request = new HttpRequestMessage(
                method, new Uri(pathAndQuery, UriKind.Relative));
            SignRequest(request, credentials, method, pathAndQuery, bodyBytes);

            if (body is not null)
            {
                var content = new ByteArrayContent(bodyBytes);
                content.Headers.ContentType = new("application/json");
                request.Content = content;
            }

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, method.Method, (int)response.StatusCode, pathAndQuery,
                    ExtractErrorDetail(payload));
                return null;
            }

            return JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, $"{method.Method} {pathAndQuery}", ex);
            return null;
        }
    }

    /// <summary>
    /// Signs a request per Smoobu's HMAC scheme: the canonical string is
    /// <c>METHOD\nPATH\nSORTED_QUERY\nTIMESTAMP\nNONCE\nSHA256(body)\nAPI_KEY</c>,
    /// signed with HMAC-SHA256 over the API secret and sent Base64-encoded in
    /// <c>X-Signature</c> alongside <c>X-API-Key</c>, <c>X-Timestamp</c>, and
    /// <c>X-Nonce</c>.
    /// </summary>
    private static void SignRequest(
        HttpRequestMessage request,
        ChannelCredentials credentials,
        HttpMethod method,
        string pathAndQuery,
        byte[] bodyBytes)
    {
        var (path, query) = SplitPathAndQuery(pathAndQuery);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        var nonce = Guid.NewGuid().ToString();
        // Smoobu expects the body hash as lowercase hex (openssl dgst output).
        var bodyHash = Convert.ToHexStringLower(SHA256.HashData(bodyBytes));

        var canonical = string.Join('\n',
            method.Method.ToUpperInvariant(),
            path,
            CanonicalizeQuery(query),
            timestamp,
            nonce,
            bodyHash,
            credentials.ExternalAccountId);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(credentials.Secret!));
        var signature = Convert.ToBase64String(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical)));

        request.Headers.TryAddWithoutValidation("X-API-Key", credentials.ExternalAccountId);
        request.Headers.TryAddWithoutValidation("X-Timestamp", timestamp);
        request.Headers.TryAddWithoutValidation("X-Nonce", nonce);
        request.Headers.TryAddWithoutValidation("X-Signature", signature);
    }

    private static (string Path, string Query) SplitPathAndQuery(string pathAndQuery)
    {
        var separator = pathAndQuery.IndexOf('?', StringComparison.Ordinal);
        return separator < 0
            ? (pathAndQuery, string.Empty)
            : (pathAndQuery[..separator], pathAndQuery[(separator + 1)..]);
    }

    /// <summary>Query parameters must be sorted alphabetically before signing.</summary>
    private static string CanonicalizeQuery(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            return string.Empty;
        }

        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        Array.Sort(pairs, StringComparer.Ordinal);
        return string.Join('&', pairs);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool HasCredentials(ChannelCredentials credentials)
        => !string.IsNullOrWhiteSpace(credentials.ExternalAccountId)
           && !string.IsNullOrWhiteSpace(credentials.Secret);

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

    private static string? ExtractErrorDetail(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return Str(doc.RootElement, "detail")
                   ?? Str(doc.RootElement, "title")
                   ?? Str(doc.RootElement, "message");
        }
        catch (JsonException)
        {
            return null;
        }
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

    private static decimal Money(long cents) => cents / 100m;

    private static long SumByType(IReadOnlyList<ChannelOrderItem> items, params string[] needles)
        => items
            .Where(i => needles.Any(n =>
                i.Type.Contains(n, StringComparison.OrdinalIgnoreCase)
                || i.Name.Contains(n, StringComparison.OrdinalIgnoreCase)))
            .Sum(i => i.AmountCents);

    private static string Iso(DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTime? ParseUtc(string? raw)
        => DateTime.TryParse(raw, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)
            ? dt
            : null;

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Smoobu] {Method} skipped — connection is missing API key or API secret")]
    private static partial void LogMissingSecret(ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Smoobu] {Method} got HTTP {StatusCode} for {RequestUri}: {Detail}")]
    private static partial void LogHttpError(
        ILogger logger, string method, int statusCode, string requestUri, string? detail);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Smoobu] {Operation} failed")]
    private static partial void LogRequestException(ILogger logger, string operation, Exception ex);
}
