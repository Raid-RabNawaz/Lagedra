using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Channels.Hostaway;

/// <summary>
/// Hostaway implementation of <see cref="IChannelProvider"/> against the
/// Hostaway Public API v1 (REST/JSON). Per-connection credentials are the
/// host's Hostaway account ID (<see cref="ChannelCredentials.ExternalAccountId"/>)
/// and API client secret (<see cref="ChannelCredentials.Secret"/>); we exchange
/// those for a Bearer access token (client-credentials grant) and cache it.
/// </summary>
public sealed partial class HostawayChannelProvider(
    HttpClient httpClient,
    IOptions<HostawayChannelSettings> settings,
    IMemoryCache cache,
    ILogger<HostawayChannelProvider> logger) : IChannelProvider
{
    private readonly HostawayChannelSettings _settings = settings.Value;

    public string ProviderKey => "hostaway";

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

        var snapshots = new List<ChannelListingSnapshot>();
        var offset = 0;
        while (true)
        {
            var path =
                $"/v1/listings?limit={_settings.PageSize}&offset={offset}&includeResources=1";
            using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
            if (doc is null)
            {
                break;
            }

            if (!TryGetResultArray(doc.RootElement, out var items) || items.GetArrayLength() == 0)
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

            offset += _settings.PageSize;
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
        if (!HasSecret(credentials))
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
        if (!HasSecret(credentials))
        {
            LogMissingSecret(logger, nameof(CheckAvailabilityAsync));
            return new ChannelAvailabilityResult(false, "NotConfigured");
        }

        if (query.CheckOut <= query.CheckIn)
        {
            return new ChannelAvailabilityResult(false, "InvalidDates");
        }

        // Hostaway calendars are night-based; checkout day itself need not be "available".
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
        if (!HasSecret(credentials))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "NotConfigured",
                ErrorMessage: "Hostaway account credentials are not configured on this connection.");
        }

        if (!long.TryParse(request.ExternalListingId, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var listingMapId))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "InvalidListingId",
                ErrorMessage: "Hostaway listing id must be numeric.");
        }

        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency;
        var totalCents = request.OrderItems.Sum(i => i.AmountCents);
        var cleaningCents = SumByType(request.OrderItems, "cleaning", "CLEANING");
        var depositCents = SumByType(request.OrderItems, "deposit", "DEPOSIT", "security");
        var taxCents = SumByType(request.OrderItems, "tax", "TAX");

        var body = new Dictionary<string, object?>
        {
            ["channelId"] = _settings.DefaultChannelId,
            ["listingMapId"] = listingMapId,
            ["guestFirstName"] = request.Guest.FirstName,
            ["guestLastName"] = request.Guest.LastName,
            ["guestName"] = $"{request.Guest.FirstName} {request.Guest.LastName}".Trim(),
            ["guestEmail"] = request.Guest.Email,
            ["phone"] = request.Guest.Phone,
            ["numberOfGuests"] = Math.Max(1, request.Adults + request.Children),
            ["adults"] = Math.Max(1, request.Adults),
            ["children"] = request.Children,
            ["pets"] = request.Pets,
            ["arrivalDate"] = request.CheckIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["departureDate"] = request.CheckOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["totalPrice"] = Money(totalCents),
            ["currency"] = currency,
            ["isPaid"] = 1,
            ["source"] = "Lagedra",
            ["comment"] = string.IsNullOrWhiteSpace(request.Message)
                ? $"Lagedra booking {request.TrackingReference}"
                : request.Message,
        };

        if (cleaningCents > 0)
        {
            body["cleaningFee"] = Money(cleaningCents);
        }

        if (depositCents > 0)
        {
            body["securityDepositFee"] = Money(depositCents);
        }

        if (taxCents > 0)
        {
            body["taxAmount"] = Money(taxCents);
        }

        try
        {
            using var response = await SendAuthorizedAsync(
                    credentials,
                    HttpMethod.Post,
                    "/v1/reservations",
                    JsonContent.Create(body),
                    ct)
                .ConfigureAwait(false);

            var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, nameof(PushBookingAsync), (int)response.StatusCode, "/v1/reservations");
                return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                    ErrorMessage: ExtractFailMessage(payload) ?? "Hostaway rejected the reservation.");
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            if (!IsSuccessStatus(doc.RootElement))
            {
                return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                    ErrorMessage: ExtractFailMessage(payload) ?? "Hostaway returned fail status.");
            }

            var externalId = TryReadResultId(doc.RootElement);
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return new ChannelBookingPushResult(false, ErrorCode: "NoBookingId",
                    ErrorMessage: "Hostaway did not return a reservation id.");
            }

            return new ChannelBookingPushResult(true, ExternalBookingId: externalId);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, nameof(PushBookingAsync), ex);
            return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                ErrorMessage: "Hostaway create-reservation request failed.");
        }
    }

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

        var since = changedSinceUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var updates = new List<ChannelBookingUpdate>();
        var offset = 0;

        while (true)
        {
            var path =
                $"/v1/reservations?limit={_settings.PageSize}&offset={offset}" +
                $"&latestActivityStart={Uri.EscapeDataString(since)}&sortOrder=updatedOn";
            using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
            if (doc is null)
            {
                break;
            }

            if (!TryGetResultArray(doc.RootElement, out var items) || items.GetArrayLength() == 0)
            {
                break;
            }

            foreach (var item in items.EnumerateArray())
            {
                var id = item.TryGetProperty("id", out var idEl)
                    ? idEl.ToString()
                    : null;
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                var status = item.TryGetProperty("status", out var statusEl)
                    ? statusEl.GetString()
                    : null;
                var changedAt = ParseUtc(
                    item.TryGetProperty("latestActivityOn", out var act) ? act.GetString() : null)
                    ?? ParseUtc(item.TryGetProperty("updatedOn", out var upd) ? upd.GetString() : null)
                    ?? DateTime.UtcNow;

                updates.Add(new ChannelBookingUpdate(id, NormalizeBookingStatus(status), changedAt));
            }

            if (items.GetArrayLength() < _settings.PageSize)
            {
                break;
            }

            offset += _settings.PageSize;
        }

        return updates;
    }

    /// <summary>
    /// Ensures a Hostaway unified webhook exists for <paramref name="callbackUrl"/>
    /// on this account (idempotent — skips create when URL already registered).
    /// </summary>
    public async Task<HostawayWebhookEnsureResult> EnsureUnifiedWebhookAsync(
        ChannelCredentials credentials,
        Uri callbackUrl,
        string? login,
        string? password,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(callbackUrl);
        if (!HasSecret(credentials))
        {
            LogMissingSecret(logger, nameof(EnsureUnifiedWebhookAsync));
            return HostawayWebhookEnsureResult.Skipped;
        }

        var target = NormalizeUrl(callbackUrl);
        using (var existing = await GetJsonAsync(credentials, "/v1/webhooks/unifiedWebhooks", ct)
                   .ConfigureAwait(false))
        {
            if (existing is not null
                && TryGetResultArray(existing.RootElement, out var hooks))
            {
                foreach (var hook in hooks.EnumerateArray())
                {
                    var url = hook.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(url)
                        && string.Equals(NormalizeUrl(new Uri(url)), target, StringComparison.OrdinalIgnoreCase))
                    {
                        LogWebhookAlreadyRegistered(logger, target);
                        return HostawayWebhookEnsureResult.AlreadyPresent;
                    }
                }
            }
        }

        var body = new Dictionary<string, object?>
        {
            ["isEnabled"] = 1,
            ["url"] = callbackUrl.AbsoluteUri,
            ["login"] = string.IsNullOrWhiteSpace(login) ? null : login,
            ["password"] = string.IsNullOrWhiteSpace(password) ? null : password,
        };

        try
        {
            using var response = await SendAuthorizedAsync(
                    credentials,
                    HttpMethod.Post,
                    "/v1/webhooks/unifiedWebhooks",
                    JsonContent.Create(body),
                    ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, nameof(EnsureUnifiedWebhookAsync), (int)response.StatusCode,
                    "/v1/webhooks/unifiedWebhooks");
                return HostawayWebhookEnsureResult.Failed;
            }

            LogWebhookRegistered(logger, target);
            return HostawayWebhookEnsureResult.Created;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, nameof(EnsureUnifiedWebhookAsync), ex);
            return HostawayWebhookEnsureResult.Failed;
        }
    }

    private static string NormalizeUrl(Uri uri)
        => uri.GetLeftPart(UriPartial.Path).TrimEnd('/');

    // ── Calendar ─────────────────────────────────────────────────────────────

    private async Task<Dictionary<DateOnly, bool>> FetchCalendarDaysAsync(
        ChannelCredentials credentials,
        string externalListingId,
        DateOnly start,
        DateOnly end,
        CancellationToken ct)
    {
        var result = new Dictionary<DateOnly, bool>();
        // Hostaway calendars are typically requested in month-sized windows.
        for (var windowStart = start; windowStart <= end; windowStart = windowStart.AddDays(90))
        {
            var windowEnd = windowStart.AddDays(89);
            if (windowEnd > end)
            {
                windowEnd = end;
            }

            var path =
                $"/v1/listings/{Uri.EscapeDataString(externalListingId)}/calendar" +
                $"?startDate={windowStart:yyyy-MM-dd}&endDate={windowEnd:yyyy-MM-dd}";
            using var doc = await GetJsonAsync(credentials, path, ct).ConfigureAwait(false);
            if (doc is null)
            {
                // Request-level failure (e.g. 403 on a listing we can no longer
                // access). The remaining windows fail identically and each one
                // costs a rate-limit delay, so stop instead of hammering the
                // same forbidden calendar four more times per sync.
                break;
            }

            if (!TryGetResultArray(doc.RootElement, out var days))
            {
                continue;
            }

            foreach (var day in days.EnumerateArray())
            {
                var dateRaw = day.TryGetProperty("date", out var dateEl) ? dateEl.GetString() : null;
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
        if (day.TryGetProperty("isAvailable", out var avail))
        {
            if (avail.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                return avail.GetBoolean();
            }

            if (avail.ValueKind == JsonValueKind.Number)
            {
                return avail.GetInt32() == 1;
            }
        }

        var status = day.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
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
        if (!item.TryGetProperty("id", out var idEl))
        {
            return null;
        }

        var externalId = idEl.ToString();
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        var specialStatus = item.TryGetProperty("specialStatus", out var ss) ? ss.GetString() : null;
        if (string.Equals(specialStatus, "archived", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var title = FirstNonEmpty(
            Str(item, "name"),
            Str(item, "externalListingName"),
            Str(item, "internalListingName"),
            externalId)!;

        var price = Dec(item, "price");
        long? nightlyCents = price is > 0
            ? (long)Math.Round(price.Value * 100m, MidpointRounding.AwayFromZero)
            : null;
        long? monthlyCents = nightlyCents.HasValue ? nightlyCents.Value * 30 : null;

        var deposit = Dec(item, "refundableDamageDeposit");
        long? depositCents = deposit is > 0
            ? (long)Math.Round(deposit.Value * 100m, MidpointRounding.AwayFromZero)
            : null;

        var sqm = Dec(item, "squareMeters");
        int? squareFootage = sqm is > 0
            ? (int)Math.Round(sqm.Value * 10.7639m, MidpointRounding.AwayFromZero)
            : null;

        var address = new ChannelAddress(
            Line1: Str(item, "street") ?? Str(item, "address"),
            City: Str(item, "city"),
            State: Str(item, "state"),
            PostalCode: Str(item, "zipcode"),
            Country: Str(item, "countryCode") ?? Str(item, "country"));

        var photos = ParsePhotos(item);
        var amenities = ParseAmenityCodes(item);

        return new ChannelListingSnapshot(
            ExternalListingId: externalId,
            Title: title,
            Description: Str(item, "description"),
            MonthlyRentCents: monthlyCents,
            NightlyRateCents: nightlyCents,
            Currency: Str(item, "currencyCode") ?? "USD",
            MinStayNights: Int(item, "minNights"),
            MaxStayNights: Int(item, "maxNights"),
            Bedrooms: Int(item, "bedroomsNumber"),
            Bathrooms: Dec(item, "bathroomsNumber"),
            SquareFootage: squareFootage,
            DepositCents: depositCents,
            Latitude: Dbl(item, "lat"),
            Longitude: Dbl(item, "lng"),
            PropertyType: MapPropertyType(Int(item, "propertyTypeId"), Str(item, "roomType")),
            Address: address,
            AmenityCodes: amenities,
            Photos: photos);
    }

    private static List<ChannelPhoto> ParsePhotos(JsonElement item)
    {
        if (!item.TryGetProperty("listingImages", out var images)
            || images.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var photos = new List<ChannelPhoto>();
        foreach (var img in images.EnumerateArray())
        {
            var urlText = Str(img, "url");
            if (urlText is null || !Uri.TryCreate(urlText, UriKind.Absolute, out var uri))
            {
                continue;
            }

            var photoId = img.TryGetProperty("id", out var idEl)
                ? idEl.ToString()
                : Guid.NewGuid().ToString("n");
            photos.Add(new ChannelPhoto(photoId, uri, Str(img, "caption")));
        }

        return photos;
    }

    private static List<string> ParseAmenityCodes(JsonElement item)
    {
        if (!item.TryGetProperty("listingAmenities", out var amenities)
            || amenities.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var codes = new List<string>();
        foreach (var a in amenities.EnumerateArray())
        {
            var name = Str(a, "amenityName")
                ?? (a.TryGetProperty("amenityId", out var idEl) ? idEl.ToString() : null);
            if (!string.IsNullOrWhiteSpace(name))
            {
                codes.Add(name!);
            }
        }

        return codes;
    }

    private static string MapPropertyType(int? propertyTypeId, string? roomType)
    {
        if (!string.IsNullOrWhiteSpace(roomType))
        {
            if (roomType.Contains("entire", StringComparison.OrdinalIgnoreCase)
                && roomType.Contains("home", StringComparison.OrdinalIgnoreCase))
            {
                // Fall through to propertyTypeId for house vs apartment.
            }
            else if (roomType.Contains("private", StringComparison.OrdinalIgnoreCase))
            {
                return "room";
            }
            else if (roomType.Contains("shared", StringComparison.OrdinalIgnoreCase))
            {
                return "room";
            }
        }

        // Hostaway propertyType dictionary (common subset).
        return propertyTypeId switch
        {
            1 => "apartment",
            2 => "house",
            3 => "other", // bed & breakfast
            4 => "other", // boutique hotel
            5 => "cabin",
            6 => "other", // chalet
            7 => "condo",
            8 => "cottage",
            9 => "other", // guest house
            10 => "other", // hostel
            11 => "other", // hotel
            12 => "other", // lodge
            13 => "villa",
            14 => "townhouse",
            15 => "studio",
            16 => "other", // boat
            17 => "other", // camper/rv
            18 => "other", // tent
            19 => "other", // tiny house
            20 => "loft",
            _ => "other",
        };
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

            var doc = JsonDocument.Parse(payload);
            if (!IsSuccessStatus(doc.RootElement))
            {
                LogHttpError(logger, "GET", (int)response.StatusCode, path);
                doc.Dispose();
                return null;
            }

            return doc;
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
        // Buffer the body up-front so we can retry after a token refresh.
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
        var cacheKey = $"hostaway:token:{credentials.ExternalAccountId}";
        if (!forceRefresh
            && cache.TryGetValue(cacheKey, out string? cached)
            && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        if (string.IsNullOrWhiteSpace(credentials.Secret))
        {
            throw new InvalidOperationException("Hostaway client secret is required.");
        }

        if (!int.TryParse(credentials.ExternalAccountId, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var clientId))
        {
            throw new InvalidOperationException("Hostaway account ID must be numeric.");
        }

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId.ToString(CultureInfo.InvariantCulture),
            ["client_secret"] = credentials.Secret,
            ["scope"] = "general",
        });

        using var response = await httpClient
            .PostAsync(new Uri("/v1/accessTokens", UriKind.Relative), form, ct)
            .ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            LogHttpError(logger, "accessTokens", (int)response.StatusCode, "/v1/accessTokens");
            throw new InvalidOperationException(
                ExtractFailMessage(payload) ?? "Failed to obtain Hostaway access token.");
        }

        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;
        var token = root.TryGetProperty("access_token", out var tokenEl)
            ? tokenEl.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Hostaway access token response was empty.");
        }

        // Hostaway tokens are long-lived (~24 months). Cache with a safety margin.
        var expiresIn = root.TryGetProperty("expires_in", out var expEl) && expEl.TryGetInt32(out var seconds)
            ? seconds
            : 60 * 60 * 24 * 30;
        var ttl = TimeSpan.FromSeconds(Math.Max(60, expiresIn - 3600));
        cache.Set(cacheKey, token, ttl);

        // Newly issued tokens are valid after ~1 second per Hostaway docs.
        await Task.Delay(1100, ct).ConfigureAwait(false);
        return token;
    }

    // ── JSON helpers ─────────────────────────────────────────────────────────

    private static bool HasSecret(ChannelCredentials credentials)
        => !string.IsNullOrWhiteSpace(credentials.ExternalAccountId)
           && !string.IsNullOrWhiteSpace(credentials.Secret);

    private static bool TryGetResultArray(JsonElement root, out JsonElement array)
    {
        if (root.TryGetProperty("result", out array) && array.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        array = default;
        return false;
    }

    private static bool IsSuccessStatus(JsonElement root)
        => !root.TryGetProperty("status", out var status)
           || !string.Equals(status.GetString(), "fail", StringComparison.OrdinalIgnoreCase);

    private static string? TryReadResultId(JsonElement root)
    {
        if (!root.TryGetProperty("result", out var result))
        {
            return null;
        }

        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("id", out var id))
        {
            return id.ToString();
        }

        return null;
    }

    private static string? ExtractFailMessage(string? payload)
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

            if (doc.RootElement.TryGetProperty("result", out var result)
                && result.ValueKind == JsonValueKind.String)
            {
                return result.GetString();
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

    private static string NormalizeBookingStatus(string? status) => (status ?? string.Empty).ToUpperInvariant() switch
    {
        "NEW" or "MODIFIED" or "OWNERSTAY" => "confirmed",
        "CANCELLED" or "DECLINED" or "EXPIRED" or "INQUIRYDENIED" or "INQUIRYNOTPOSSIBLE" or "INQUIRYTIMEDOUT" =>
            "cancelled",
        "PENDING" or "AWAITINGPAYMENT" or "UNCONFIRMED" or "AWAITINGGUESTVERIFICATION"
            or "INQUIRY" or "INQUIRYPREAPPROVED" or "UNKNOWN" => "pending",
        _ => "pending",
    };

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Hostaway] {Method} skipped — connection is missing account ID or client secret")]
    private static partial void LogMissingSecret(ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Hostaway] {Method} got HTTP {StatusCode} for {RequestUri}")]
    private static partial void LogHttpError(ILogger logger, string method, int statusCode, string requestUri);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Hostaway] {Operation} failed")]
    private static partial void LogRequestException(ILogger logger, string operation, Exception ex);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[Hostaway] unified webhook already registered for {CallbackUrl}")]
    private static partial void LogWebhookAlreadyRegistered(ILogger logger, string callbackUrl);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[Hostaway] registered unified webhook for {CallbackUrl}")]
    private static partial void LogWebhookRegistered(ILogger logger, string callbackUrl);
}
