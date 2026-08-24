using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Channels.Hosthub;

/// <summary>
/// Hosthub public API (<c>/api/2019-03-01</c>) implementation of
/// <see cref="IChannelProvider"/>. Per-connection credentials are the Hosthub
/// account owner's API key on <see cref="ChannelCredentials.Secret"/>. Auth is
/// ApiKeyAuth: the key is sent as <c>Authorization</c> (raw, then Bearer on
/// 401/403). Hosts paste the key in Lagedra; nothing is shared at the platform
/// level besides <see cref="HosthubChannelSettings.BaseUrl"/>.
/// </summary>
public sealed partial class HosthubChannelProvider(
    HttpClient httpClient,
    IOptions<HosthubChannelSettings> settings,
    IMemoryCache cache,
    ILogger<HosthubChannelProvider> logger) : IChannelProvider
{
    private enum AuthScheme
    {
        Raw,
        Bearer,
    }

    private readonly HosthubChannelSettings _settings = settings.Value;

    public string ProviderKey => "hosthub";

    // ── Listings ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ChannelListingSnapshot>> PullListingsAsync(
        ChannelCredentials credentials,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (!HasSecret(credentials))
        {
            LogMissingSecret(logger, nameof(PullListingsAsync));
            return [];
        }

        var rentals = await EnumerateCollectionAsync(credentials, ApiPath("rentals"), ct)
            .ConfigureAwait(false);
        var snapshots = new List<ChannelListingSnapshot>();
        foreach (var rental in rentals)
        {
            var snapshot = ParseRental(rental);
            if (snapshot is null)
            {
                continue;
            }

            snapshots.Add(await EnrichListingAsync(credentials, snapshot, ct).ConfigureAwait(false));
        }

        return snapshots;
    }

    private async Task<ChannelListingSnapshot> EnrichListingAsync(
        ChannelCredentials credentials,
        ChannelListingSnapshot snapshot,
        CancellationToken ct)
    {
        var id = snapshot.ExternalListingId;
        using var detail = await GetJsonAsync(credentials, ApiPath($"rentals/{Uri.EscapeDataString(id)}"), ct)
            .ConfigureAwait(false);
        if (detail is not null)
        {
            snapshot = ParseRental(detail.RootElement) ?? snapshot;
        }

        if (snapshot.NightlyRateCents is > 0)
        {
            return snapshot;
        }

        return await TryApplyDefaultRateAsync(credentials, snapshot, ct).ConfigureAwait(false);
    }

    private async Task<ChannelListingSnapshot> TryApplyDefaultRateAsync(
        ChannelCredentials credentials,
        ChannelListingSnapshot snapshot,
        CancellationToken ct)
    {
        var id = snapshot.ExternalListingId;
        using var plansDoc = await GetJsonAsync(
                credentials, ApiPath($"rentals/{Uri.EscapeDataString(id)}/rate-plans"), ct)
            .ConfigureAwait(false);
        if (plansDoc is null || !TryGetDataArray(plansDoc.RootElement, out var plans))
        {
            return snapshot;
        }

        string? planId = null;
        foreach (var plan in plans.EnumerateArray())
        {
            var candidate = Str(plan, "id");
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            if (plan.TryGetProperty("default", out var def)
                && def.ValueKind == JsonValueKind.True)
            {
                planId = candidate;
                break;
            }

            planId ??= candidate;
        }

        if (string.IsNullOrWhiteSpace(planId))
        {
            return snapshot;
        }

        using var ratesDoc = await GetJsonAsync(
                credentials, ApiPath($"rate-plans/{Uri.EscapeDataString(planId)}/rates"), ct)
            .ConfigureAwait(false);
        if (ratesDoc is null || !TryGetDataArray(ratesDoc.RootElement, out var rates))
        {
            return snapshot;
        }

        foreach (var day in rates.EnumerateArray())
        {
            var (cents, currency) = ReadMoney(day, "amount");
            if (cents is not > 0)
            {
                continue;
            }

            return snapshot with
            {
                NightlyRateCents = cents,
                MonthlyRentCents = cents.Value * 30,
                Currency = currency ?? snapshot.Currency,
                MinStayNights = Int(day, "minimum_length_of_stay") ?? snapshot.MinStayNights,
                MaxStayNights = Int(day, "maximum_length_of_stay") ?? snapshot.MaxStayNights,
            };
        }

        return snapshot;
    }

    // ── Availability ─────────────────────────────────────────────────────────

    public async Task<ChannelAvailabilityCalendar> PullAvailabilityAsync(
        ChannelCredentials credentials,
        string externalListingId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        var empty = new ChannelAvailabilityCalendar(externalListingId, []);
        if (!HasSecret(credentials))
        {
            LogMissingSecret(logger, nameof(PullAvailabilityAsync));
            return empty;
        }

        var start = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var end = start.AddDays(Math.Max(30, _settings.AvailabilityLookaheadDays));
        var days = await BuildAvailabilityDaysAsync(credentials, externalListingId, start, end, ct)
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
        if (!HasSecret(credentials))
        {
            LogMissingSecret(logger, nameof(CheckAvailabilityAsync));
            return new ChannelAvailabilityResult(false, "NotConfigured");
        }

        if (query.CheckOut <= query.CheckIn)
        {
            return new ChannelAvailabilityResult(false, "InvalidDates");
        }

        if (string.IsNullOrWhiteSpace(query.ExternalListingId))
        {
            return new ChannelAvailabilityResult(false, "InvalidListingId");
        }

        var lastNight = query.CheckOut.AddDays(-1);
        var days = await BuildAvailabilityDaysAsync(
                credentials, query.ExternalListingId, query.CheckIn, lastNight, ct)
            .ConfigureAwait(false);
        if (days.Count == 0)
        {
            // Empty calendar: no blocking events in range.
            return new ChannelAvailabilityResult(true);
        }

        for (var d = query.CheckIn; d <= lastNight; d = d.AddDays(1))
        {
            if (days.TryGetValue(d, out var available) && !available)
            {
                return new ChannelAvailabilityResult(false, "Unavailable");
            }
        }

        return new ChannelAvailabilityResult(true);
    }

    private async Task<Dictionary<DateOnly, bool>> BuildAvailabilityDaysAsync(
        ChannelCredentials credentials,
        string externalListingId,
        DateOnly start,
        DateOnly end,
        CancellationToken ct)
    {
        var days = new Dictionary<DateOnly, bool>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            days[d] = true;
        }

        var path = ApiPath($"rentals/{Uri.EscapeDataString(externalListingId)}/calendar-events");
        var events = await EnumerateCollectionAsync(credentials, path, ct).ConfigureAwait(false);
        foreach (var ev in events)
        {
            if (!IsBlockingEvent(ev))
            {
                continue;
            }

            var from = ParseDate(Str(ev, "date_from"));
            var to = ParseDate(Str(ev, "date_to"));
            if (from is null || to is null || to.Value <= from.Value)
            {
                continue;
            }

            // date_to is checkout (exclusive), matching Hosthub's nights field.
            for (var d = from.Value; d < to.Value && d <= end; d = d.AddDays(1))
            {
                if (d >= start)
                {
                    days[d] = false;
                }
            }
        }

        return days;
    }

    // ── Booking push ─────────────────────────────────────────────────────────

    public async Task<ChannelBookingPushResult> PushBookingAsync(
        ChannelCredentials credentials,
        ChannelBookingPushRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(request);
        if (!HasSecret(credentials))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "NotConfigured",
                ErrorMessage: "Hosthub API key is not configured on this connection.");
        }

        if (string.IsNullOrWhiteSpace(request.ExternalListingId))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "InvalidListingId",
                ErrorMessage: "Hosthub rental id is required.");
        }

        if (request.CheckOut <= request.CheckIn)
        {
            return new ChannelBookingPushResult(false, ErrorCode: "InvalidDates",
                ErrorMessage: "Check-out must be after check-in.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (request.CheckIn > today.AddDays(730) || request.CheckOut > today.AddDays(730)
            || request.CheckOut.DayNumber - request.CheckIn.DayNumber > 365)
        {
            return new ChannelBookingPushResult(false, ErrorCode: "InvalidDates",
                ErrorMessage: "Hosthub only accepts stays within 730 days and at most 365 nights.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency;
        var totalCents = request.OrderItems.Sum(i => i.AmountCents);
        var cleaningCents = SumByType(request.OrderItems, "cleaning");
        var guestName = Truncate($"{request.Guest.FirstName} {request.Guest.LastName}".Trim(), 200);

        var body = new Dictionary<string, object?>
        {
            ["type"] = "Booking",
            ["date_from"] = Iso(request.CheckIn),
            ["date_to"] = Iso(request.CheckOut),
            ["reservation_id"] = Truncate(request.TrackingReference, 200),
            ["guest_name"] = string.IsNullOrWhiteSpace(guestName) ? "Lagedra Guest" : guestName,
            ["guest_adults"] = Math.Max(1, request.Adults),
            ["guest_children"] = Math.Max(0, request.Children),
            ["guest_email"] = Truncate(request.Guest.Email, 200),
            ["notes"] = string.IsNullOrWhiteSpace(request.Message)
                ? $"Lagedra booking {request.TrackingReference}"
                : request.Message,
            ["booking_value"] = Money(totalCents, currency),
            ["guest_paid"] = Money(totalCents, currency),
        };

        if (!string.IsNullOrWhiteSpace(request.Guest.Phone))
        {
            body["guest_phone"] = Truncate(request.Guest.Phone, 20);
        }

        if (cleaningCents > 0)
        {
            body["cleaning_fee"] = Money(cleaningCents, currency);
        }

        if (!string.IsNullOrWhiteSpace(_settings.SourceId))
        {
            body["source_id"] = _settings.SourceId.Trim();
        }

        var path = ApiPath($"rentals/{Uri.EscapeDataString(request.ExternalListingId)}/calendar-events");
        try
        {
            using var response = await SendAuthorizedAsync(
                    credentials, HttpMethod.Post, path, JsonContent.Create(body), ct)
                .ConfigureAwait(false);
            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, nameof(PushBookingAsync), (int)response.StatusCode, path);
                return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                    ErrorMessage: ExtractErrorMessage(payload) ?? "Hosthub rejected the booking.");
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            var externalId = Str(doc.RootElement, "id");
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return new ChannelBookingPushResult(false, ErrorCode: "NoBookingId",
                    ErrorMessage: "Hosthub did not return a calendar event id.");
            }

            return new ChannelBookingPushResult(true, ExternalBookingId: externalId);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, nameof(PushBookingAsync), ex);
            return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                ErrorMessage: "Hosthub create-booking request failed.");
        }
    }

    // ── Booking updates ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<ChannelBookingUpdate>> PullBookingUpdatesAsync(
        ChannelCredentials credentials,
        DateTime changedSinceUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (!HasSecret(credentials))
        {
            LogMissingSecret(logger, nameof(PullBookingUpdatesAsync));
            return [];
        }

        var sinceUnix = new DateTimeOffset(DateTime.SpecifyKind(changedSinceUtc, DateTimeKind.Utc))
            .ToUnixTimeSeconds();
        var path = ApiPath($"calendar-events?updated_gt={sinceUnix}&is_visible=all");
        var events = await EnumerateCollectionAsync(credentials, path, ct).ConfigureAwait(false);

        var updates = new List<ChannelBookingUpdate>();
        foreach (var ev in events)
        {
            var update = ParseBookingUpdate(ev);
            if (update is not null)
            {
                updates.Add(update);
            }
        }

        return updates;
    }

    private static ChannelBookingUpdate? ParseBookingUpdate(JsonElement ev)
    {
        var id = Str(ev, "id");
        if (string.IsNullOrWhiteSpace(id) || IsHold(Str(ev, "type")))
        {
            return null;
        }

        var changedAt = ParseUnix(ev, "updated")
                        ?? ParseUnix(ev, "created")
                        ?? ParseUnix(ev, "cancelled_at")
                        ?? DateTime.UtcNow;
        var status = IsCancelled(ev) ? "cancelled" : "confirmed";
        return new ChannelBookingUpdate(id, status, changedAt);
    }

    // ── Listing parsing ──────────────────────────────────────────────────────

    private ChannelListingSnapshot? ParseRental(JsonElement item)
    {
        var externalId = Str(item, "id");
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        var status = Str(item, "status");
        if (status is not null
            && (status.Equals("inactive", StringComparison.OrdinalIgnoreCase)
                || status.Equals("archived", StringComparison.OrdinalIgnoreCase)
                || status.Equals("deleted", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var title = FirstNonEmpty(Str(item, "name"), externalId)!;
        var description = FirstNonEmpty(Str(item, "description"), Str(item, "summary"));
        var (nightlyCents, moneyCurrency) = ReadMoney(item, "price") is { Item1: > 0 } priced
            ? priced
            : ReadMoney(item, "nightly_rate");

        var address = new ChannelAddress(
            Line1: FirstNonEmpty(Str(item, "address"), Str(item, "street"), Str(item, "address_line1")),
            City: Str(item, "city"),
            State: FirstNonEmpty(Str(item, "state"), Str(item, "region")),
            PostalCode: FirstNonEmpty(Str(item, "postal_code"), Str(item, "zip"), Str(item, "zip_code")),
            Country: Str(item, "country"));
        if (address.Line1 is null && address.City is null && address.Country is null)
        {
            address = null;
        }

        return new ChannelListingSnapshot(
            ExternalListingId: externalId,
            Title: title,
            Description: description,
            MonthlyRentCents: nightlyCents is > 0 ? nightlyCents.Value * 30 : null,
            NightlyRateCents: nightlyCents is > 0 ? nightlyCents : null,
            Currency: moneyCurrency ?? Str(item, "currency") ?? "USD",
            MinStayNights: Int(item, "minimum_length_of_stay") ?? Int(item, "min_nights"),
            MaxStayNights: Int(item, "maximum_length_of_stay") ?? Int(item, "max_nights"),
            Bedrooms: Int(item, "bedrooms") ?? Int(item, "bedroom_count"),
            Bathrooms: Dec(item, "bathrooms") ?? Dec(item, "bathroom_count"),
            SquareFootage: Int(item, "square_footage") ?? Int(item, "sqft"),
            DepositCents: ReadMoney(item, "security_deposit").Cents,
            Latitude: Dbl(item, "latitude"),
            Longitude: Dbl(item, "longitude"),
            PropertyType: MapPropertyType(
                FirstNonEmpty(Str(item, "property_type"), Str(item, "type"))),
            Address: address,
            AmenityCodes: ParseAmenities(item),
            Photos: ParsePhotos(item));
    }

    private List<ChannelPhoto> ParsePhotos(JsonElement item)
    {
        var photos = new List<ChannelPhoto>();
        if (item.TryGetProperty("photos", out var photosEl) && photosEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var img in photosEl.EnumerateArray())
            {
                var urlText = FirstNonEmpty(Str(img, "url"), Str(img, "image_path"), Str(img, "path"));
                if (TryCreatePhotoUri(urlText, out var uri))
                {
                    var id = FirstNonEmpty(Str(img, "id"), urlText) ?? Guid.NewGuid().ToString("n");
                    photos.Add(new ChannelPhoto(id, uri, Str(img, "caption")));
                }
            }
        }

        if (photos.Count == 0 && TryCreatePhotoUri(Str(item, "image_path"), out var cover))
        {
            photos.Add(new ChannelPhoto("cover", cover, null));
        }

        return photos;
    }

    private bool TryCreatePhotoUri(string? raw, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
        {
            uri = absolute;
            return true;
        }

        if (raw.StartsWith('/') && Uri.TryCreate(_settings.BaseUrl, raw, out var relative))
        {
            uri = relative;
            return true;
        }

        return false;
    }

    private static List<string> ParseAmenities(JsonElement item)
    {
        if (!item.TryGetProperty("amenities", out var amenities)
            && !item.TryGetProperty("amenity_names", out amenities))
        {
            return [];
        }

        var codes = new List<string>();
        if (amenities.ValueKind == JsonValueKind.Array)
        {
            foreach (var a in amenities.EnumerateArray())
            {
                if (a.ValueKind == JsonValueKind.String && a.GetString() is { Length: > 0 } name)
                {
                    codes.Add(name);
                }
                else if (a.ValueKind == JsonValueKind.Object)
                {
                    var label = FirstNonEmpty(Str(a, "name"), Str(a, "key"));
                    if (label is not null)
                    {
                        codes.Add(label);
                    }
                }
            }
        }

        return codes;
    }

    private static string MapPropertyType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "other";
        }

        var value = raw.ToUpperInvariant();
        if (value.Contains("APARTMENT", StringComparison.Ordinal)) return "apartment";
        if (value.Contains("CONDO", StringComparison.Ordinal)) return "condo";
        if (value.Contains("TOWNHOUSE", StringComparison.Ordinal) || value.Contains("TOWNHOME", StringComparison.Ordinal))
            return "townhouse";
        if (value.Contains("VILLA", StringComparison.Ordinal)) return "villa";
        if (value.Contains("CABIN", StringComparison.Ordinal)) return "cabin";
        if (value.Contains("COTTAGE", StringComparison.Ordinal)) return "cottage";
        if (value.Contains("STUDIO", StringComparison.Ordinal)) return "studio";
        if (value.Contains("LOFT", StringComparison.Ordinal)) return "loft";
        if (value.Contains("HOUSE", StringComparison.Ordinal) || value.Contains("HOME", StringComparison.Ordinal))
            return "house";
        return "other";
    }

    // ── HTTP / auth ──────────────────────────────────────────────────────────

    private string ApiPath(string resourceAndQuery)
    {
        if (resourceAndQuery.StartsWith("/api/", StringComparison.Ordinal))
        {
            return resourceAndQuery;
        }

        return $"/api/{_settings.ApiVersion}/{resourceAndQuery.TrimStart('/')}";
    }

    private async Task<List<JsonElement>> EnumerateCollectionAsync(
        ChannelCredentials credentials,
        string startPath,
        CancellationToken ct)
    {
        var items = new List<JsonElement>();
        var path = startPath;
        var maxPages = Math.Max(1, _settings.MaxPages);

        for (var page = 0; page < maxPages; page++)
        {
            using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
            if (doc is null)
            {
                break;
            }

            if (TryGetDataArray(doc.RootElement, out var array))
            {
                foreach (var item in array.EnumerateArray())
                {
                    items.Add(item.Clone());
                }
            }

            if (!TryGetNextPath(doc.RootElement, out var next) || string.Equals(next, path, StringComparison.Ordinal))
            {
                break;
            }

            path = next;
        }

        return items;
    }

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

            return string.IsNullOrWhiteSpace(payload) ? null : JsonDocument.Parse(payload);
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

        var preferred = GetCachedScheme(credentials) ?? AuthScheme.Raw;
        var response = await SendWithSchemeAsync(
                method, path, bodyBytes, contentType, credentials.Secret!, preferred, ct)
            .ConfigureAwait(false);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            var fallback = preferred == AuthScheme.Raw ? AuthScheme.Bearer : AuthScheme.Raw;
            response.Dispose();
            response = await SendWithSchemeAsync(
                    method, path, bodyBytes, contentType, credentials.Secret!, fallback, ct)
                .ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                CacheScheme(credentials, fallback);
            }

            return response;
        }

        if (response.IsSuccessStatusCode)
        {
            CacheScheme(credentials, preferred);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendWithSchemeAsync(
        HttpMethod method,
        string path,
        byte[]? bodyBytes,
        MediaTypeHeaderValue? contentType,
        string apiKey,
        AuthScheme scheme,
        CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.RelativeOrAbsolute));
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        ApplyAuth(request, apiKey, scheme);
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

    private static void ApplyAuth(HttpRequestMessage request, string apiKey, AuthScheme scheme)
    {
        if (scheme == AuthScheme.Bearer)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return;
        }

        request.Headers.TryAddWithoutValidation("Authorization", apiKey);
    }

    private AuthScheme? GetCachedScheme(ChannelCredentials credentials)
        => cache.TryGetValue(AuthCacheKey(credentials.Secret!), out AuthScheme scheme)
            ? scheme
            : null;

    private void CacheScheme(ChannelCredentials credentials, AuthScheme scheme)
        => cache.Set(AuthCacheKey(credentials.Secret!), scheme, TimeSpan.FromHours(12));

    private static string AuthCacheKey(string secret)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
        return $"hosthub:auth:{hash[..16]}";
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool HasSecret(ChannelCredentials credentials)
        => !string.IsNullOrWhiteSpace(credentials.Secret);

    private static bool TryGetDataArray(JsonElement root, out JsonElement array)
    {
        if (root.TryGetProperty("data", out array) && array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        array = default;
        return false;
    }

    private static bool TryGetNextPath(JsonElement root, out string next)
    {
        next = string.Empty;
        if (!root.TryGetProperty("navigation", out var nav) || nav.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var raw = Str(nav, "next");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (Uri.TryCreate(raw, UriKind.Absolute, out var absolute))
        {
            next = absolute.PathAndQuery;
            return true;
        }

        next = raw;
        return true;
    }

    private static bool IsHold(string? type)
        => type is not null && type.Contains("hold", StringComparison.OrdinalIgnoreCase);

    private static bool IsCancelled(JsonElement ev)
    {
        if (ev.TryGetProperty("is_visible", out var visible)
            && visible.ValueKind is JsonValueKind.False)
        {
            return true;
        }

        if (ev.TryGetProperty("cancelled_at", out var cancelled)
            && cancelled.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined
            && cancelled.ValueKind is not JsonValueKind.False)
        {
            if (cancelled.ValueKind == JsonValueKind.Number && cancelled.TryGetInt64(out var unix))
            {
                return unix > 0;
            }

            return cancelled.ValueKind != JsonValueKind.String
                   || !string.IsNullOrWhiteSpace(cancelled.GetString());
        }

        return false;
    }

    private static bool IsBlockingEvent(JsonElement ev)
        => !IsCancelled(ev);

    private static (long? Cents, string? Currency) ReadMoney(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var money) || money.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        var cents = money.TryGetProperty("cents", out var centsEl)
            ? (centsEl.ValueKind == JsonValueKind.Number && centsEl.TryGetInt64(out var n)
                ? n
                : long.TryParse(centsEl.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed
                    : (long?)null)
            : null;
        return (cents, Str(money, "currency"));
    }

    private static Dictionary<string, object?> Money(long cents, string currency)
        => new() { ["cents"] = cents, ["currency"] = currency };

    private static string? ExtractErrorMessage(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;
            return FirstNonEmpty(
                Str(root, "message"),
                Str(root, "error"),
                Str(root, "detail"),
                root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.Object
                    ? Str(error, "message")
                    : null);
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

    private static DateOnly? ParseDate(string? raw)
        => DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    private static DateTime? ParseUnix(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var v) || v.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (v.ValueKind == JsonValueKind.Number && v.TryGetInt64(out var seconds) && seconds > 0)
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }

        return long.TryParse(v.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
               && parsed > 0
            ? DateTimeOffset.FromUnixTimeSeconds(parsed).UtcDateTime
            : null;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static long SumByType(IReadOnlyList<ChannelOrderItem> items, params string[] needles)
        => items
            .Where(i => needles.Any(n =>
                i.Type.Contains(n, StringComparison.OrdinalIgnoreCase)
                || i.Name.Contains(n, StringComparison.OrdinalIgnoreCase)))
            .Sum(i => i.AmountCents);

    private static string Iso(DateOnly date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLen ? trimmed : trimmed[..maxLen];
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

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Hosthub] {Method} skipped — connection is missing an API key")]
    private static partial void LogMissingSecret(ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Hosthub] {Method} got HTTP {StatusCode} for {RequestUri}")]
    private static partial void LogHttpError(ILogger logger, string method, int statusCode, string requestUri);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Hosthub] {Operation} failed")]
    private static partial void LogRequestException(ILogger logger, string operation, Exception ex);
}
