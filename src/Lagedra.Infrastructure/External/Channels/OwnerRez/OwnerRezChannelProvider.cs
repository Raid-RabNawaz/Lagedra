using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>
/// OwnerRez implementation of <see cref="IChannelProvider"/> built on the
/// OwnerRez "API for Channel Integration" (HAXML content feeds + HAOLB
/// real-time quotes/bookings). Lagedra acts as a Merchant-of-Record channel:
/// it pulls advertiser (host) content via the HAXML feeds and pushes
/// already-paid bookings back via HAOLB <c>createbooking</c> using the
/// <c>paymentChannelMoR</c> payment form.
///
/// Channel-level credentials (username + key) live in
/// <see cref="OwnerRezChannelSettings"/> and are sent as HTTP Basic auth on the
/// shared <see cref="HttpClient"/>. The per-connection
/// <see cref="ChannelCredentials.ExternalAccountId"/> is the host's
/// <c>advertiserAssignedId</c> ("ora…") used to scope every feed.
/// </summary>
public sealed partial class OwnerRezChannelProvider(
    HttpClient httpClient,
    IOptions<OwnerRezChannelSettings> settings,
    ILogger<OwnerRezChannelProvider> logger) : IChannelProvider
{
    private readonly OwnerRezChannelSettings _settings = settings.Value;

    public string ProviderKey => "ownerrez";

    private bool Configured =>
        !string.IsNullOrWhiteSpace(_settings.Username) && !string.IsNullOrWhiteSpace(_settings.Key);

    public async Task<IReadOnlyList<ChannelListingSnapshot>> PullListingsAsync(
        ChannelCredentials credentials,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (!EnsureConfigured(nameof(PullListingsAsync)))
        {
            return [];
        }

        var advertiserId = credentials.ExternalAccountId;
        var indexDoc = await GetXmlAsync($"/haapi/haxml/{Uri.EscapeDataString(advertiserId)}/listingindex", ct)
            .ConfigureAwait(false);
        if (indexDoc is null)
        {
            return [];
        }

        var rates = await TryLoadRatesAsync(advertiserId, ct).ConfigureAwait(false);

        var snapshots = new List<ChannelListingSnapshot>();
        foreach (var entry in indexDoc.Descendants("listingContentIndexEntry"))
        {
            var listingExternalId = Str(entry.Element("listingExternalId"));
            if (string.IsNullOrWhiteSpace(listingExternalId) || !BoolOrTrue(entry.Element("active")))
            {
                continue;
            }

            var listingUrl = Str(entry.Element("listingUrl"))
                ?? $"/haapi/haxml/{Uri.EscapeDataString(advertiserId)}/listing/{Uri.EscapeDataString(listingExternalId)}";

            var listingDoc = await GetXmlAsync(listingUrl, ct).ConfigureAwait(false);
            if (listingDoc is null)
            {
                continue;
            }

            var snapshot = ParseListing(listingDoc, listingExternalId, rates);
            if (snapshot is not null)
            {
                snapshots.Add(snapshot);
            }
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
        if (!EnsureConfigured(nameof(PullAvailabilityAsync)))
        {
            return empty;
        }

        var idx = await GetXmlAsync(
            $"/haapi/haxml/{Uri.EscapeDataString(credentials.ExternalAccountId)}/availabilityindex", ct)
            .ConfigureAwait(false);
        if (idx is null)
        {
            return empty;
        }

        var entry = ParseIndexEntries(idx).FirstOrDefault(e =>
            string.Equals(e.ExternalId, externalListingId, StringComparison.OrdinalIgnoreCase));
        if (entry.Url is null)
        {
            return empty;
        }

        var availDoc = await GetXmlAsync(entry.Url, ct).ConfigureAwait(false);
        if (availDoc is null)
        {
            return empty;
        }

        return new ChannelAvailabilityCalendar(externalListingId, ParseAvailabilityBlocks(availDoc));
    }

    public async Task<ChannelAvailabilityResult> CheckAvailabilityAsync(
        ChannelCredentials credentials,
        ChannelAvailabilityQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(query);
        if (!EnsureConfigured(nameof(CheckAvailabilityAsync)))
        {
            return new ChannelAvailabilityResult(false, "NotConfigured");
        }

        var body = new
        {
            requestVersion = "1.0",
            systemExternalId = _settings.SystemExternalId,
            advertiserExternalId = credentials.ExternalAccountId,
            listingExternalId = query.ExternalListingId,
            dateRange = new
            {
                arrivalDate = query.CheckIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                departureDate = query.CheckOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            },
            adults = query.Adults,
            children = query.Children,
            pets = query.Pets,
            units = new[] { new { unitExternalId = query.ExternalListingId } },
        };

        try
        {
            using var response = await httpClient
                .PostAsJsonAsync("/haapi/haolbjson/fastavailability", body, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, nameof(CheckAvailabilityAsync), (int)response.StatusCode, "fastavailability");
                return new ChannelAvailabilityResult(false, "RequestFailed");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);

            if (doc.RootElement.TryGetProperty("units", out var units) && units.GetArrayLength() > 0)
            {
                var unit = units[0];
                var available = unit.TryGetProperty("available", out var a) && a.GetBoolean();
                string? errorCode = unit.TryGetProperty("errorCode", out var ec) ? ec.GetString() : null;
                return new ChannelAvailabilityResult(available, errorCode);
            }

            return new ChannelAvailabilityResult(false, "NoUnits");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            LogRequestException(logger, nameof(CheckAvailabilityAsync), ex);
            return new ChannelAvailabilityResult(false, "RequestFailed");
        }
    }

    public async Task<ChannelBookingPushResult> PushBookingAsync(
        ChannelCredentials credentials,
        ChannelBookingPushRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        ArgumentNullException.ThrowIfNull(request);
        if (!EnsureConfigured(nameof(PushBookingAsync)))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "NotConfigured",
                ErrorMessage: "OwnerRez channel credentials are not configured.");
        }

        var xml = BuildBookingXml(credentials, request);

        var responseDoc = await PostXmlAsync(WithCreds("/haapi/haolb/createbooking"), xml, ct)
            .ConfigureAwait(false);
        if (responseDoc is null)
        {
            return new ChannelBookingPushResult(false, ErrorCode: "RequestFailed",
                ErrorMessage: "OwnerRez createbooking request failed.");
        }

        var error = responseDoc.Descendants("error").FirstOrDefault();
        if (error is not null)
        {
            var type = Str(error.Element("errorType")) ?? "Error";
            var message = Str(error.Element("message")) ?? "OwnerRez rejected the booking.";
            return new ChannelBookingPushResult(false, ErrorCode: type, ErrorMessage: message);
        }

        var externalId = Str(responseDoc.Descendants("externalId").FirstOrDefault());
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return new ChannelBookingPushResult(false, ErrorCode: "NoBookingId",
                ErrorMessage: "OwnerRez did not return a booking id.");
        }

        return new ChannelBookingPushResult(true, ExternalBookingId: externalId);
    }

    public async Task<IReadOnlyList<ChannelBookingUpdate>> PullBookingUpdatesAsync(
        ChannelCredentials credentials,
        DateTime changedSinceUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(credentials);
        if (!EnsureConfigured(nameof(PullBookingUpdatesAsync)))
        {
            return [];
        }

        var advertiserId = credentials.ExternalAccountId;
        var requestXml = new XDocument(
            new XElement("bookingContentIndexRequest",
                new XElement("documentVersion", "1.4"),
                new XElement("advertiser", new XElement("assignedId", advertiserId)),
                new XElement("startDate", changedSinceUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))))
            .ToString(SaveOptions.DisableFormatting);

        var indexDoc = await PostXmlAsync(
            $"/haapi/haolb/{Uri.EscapeDataString(advertiserId)}/bookingindex", requestXml, ct)
            .ConfigureAwait(false);
        if (indexDoc is null)
        {
            return [];
        }

        var updates = new List<ChannelBookingUpdate>();
        foreach (var (externalId, url) in ParseBookingIndexEntries(indexDoc))
        {
            var detail = await GetXmlAsync(url, ct).ConfigureAwait(false);
            if (detail is null)
            {
                continue;
            }

            var status = Str(detail.Descendants("reservationStatus").FirstOrDefault())
                ?? Str(detail.Descendants("status").FirstOrDefault())
                ?? "UNKNOWN";
            var bookingId = Str(detail.Descendants("externalId").FirstOrDefault()) ?? externalId;
            var changedAt = ParseDateTime(detail.Descendants("lastUpdatedDate").FirstOrDefault())
                ?? DateTime.UtcNow;

            updates.Add(new ChannelBookingUpdate(bookingId, NormalizeBookingStatus(status), changedAt));
        }

        return updates;
    }

    // ── Parsing helpers ─────────────────────────────────────────────────────

    private static ChannelListingSnapshot? ParseListing(
        XDocument doc,
        string externalId,
        IReadOnlyDictionary<string, OwnerRezRate> rates)
    {
        var listing = doc.Root;
        if (listing is null)
        {
            return null;
        }

        var adContent = listing.Element("adContent");
        var title = FirstText(adContent?.Element("headline"))
            ?? FirstText(adContent?.Element("propertyName"))
            ?? externalId;
        var description = FirstText(adContent?.Element("description"));

        var location = listing.Element("location");
        var addrEl = location?.Element("address");
        ChannelAddress? address = addrEl is null ? null : new ChannelAddress(
            Line1: Str(addrEl.Element("addressLine1")),
            City: Str(addrEl.Element("city")),
            State: Str(addrEl.Element("stateOrProvince")),
            PostalCode: Str(addrEl.Element("postalCode")),
            Country: Str(addrEl.Element("country")));

        var latLng = location?.Element("geoCode")?.Element("latLng");
        var latitude = Dbl(latLng?.Element("latitude"));
        var longitude = Dbl(latLng?.Element("longitude"));

        var unit = listing.Element("units")?.Element("unit");
        var bedrooms = unit?.Element("bedrooms")?.Elements("bedroom").Count() ?? 0;
        var bathrooms = ParseBathrooms(unit?.Element("bathrooms"));
        var propertyType = MapPropertyType(Str(unit?.Element("propertyType")));

        var amenityCodes = unit?.Element("featureValues")?.Elements("featureValue")
            .Select(f => Str(f.Element("unitFeatureName")))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!)
            .ToList() ?? [];

        var photos = listing.Element("images")?.Elements("image")
            .Select(img =>
            {
                var uriText = Str(img.Element("uri"));
                if (uriText is null || !Uri.TryCreate(uriText, UriKind.Absolute, out var uri))
                {
                    return null;
                }

                var photoId = Str(img.Element("externalId")) ?? Guid.NewGuid().ToString("n");
                return new ChannelPhoto(photoId, uri, FirstText(img.Element("title")));
            })
            .Where(p => p is not null)
            .Select(p => p!)
            .ToList() ?? [];

        rates.TryGetValue(externalId, out var rate);
        var nightlyCents = rate?.NightlyCents;
        var monthlyCents = nightlyCents.HasValue ? nightlyCents.Value * 30 : (long?)null;

        return new ChannelListingSnapshot(
            ExternalListingId: externalId,
            Title: title,
            Description: description,
            MonthlyRentCents: monthlyCents,
            NightlyRateCents: nightlyCents,
            Currency: rate?.Currency ?? "USD",
            MinStayNights: null,
            MaxStayNights: null,
            Bedrooms: bedrooms,
            Bathrooms: bathrooms,
            SquareFootage: null,
            DepositCents: rate?.DepositCents,
            Latitude: latitude,
            Longitude: longitude,
            PropertyType: propertyType,
            Address: address,
            AmenityCodes: amenityCodes,
            Photos: photos);
    }

    private async Task<IReadOnlyDictionary<string, OwnerRezRate>> TryLoadRatesAsync(
        string advertiserId,
        CancellationToken ct)
    {
        var result = new Dictionary<string, OwnerRezRate>(StringComparer.OrdinalIgnoreCase);
        var idx = await GetXmlAsync($"/haapi/haxml/{Uri.EscapeDataString(advertiserId)}/lodgingrateindex", ct)
            .ConfigureAwait(false);
        if (idx is null)
        {
            return result;
        }

        foreach (var (externalId, url) in ParseIndexEntries(idx))
        {
            var rateDoc = await GetXmlAsync(url, ct).ConfigureAwait(false);
            var rate = rateDoc is null ? null : ParseRate(rateDoc);
            if (rate is not null)
            {
                result[externalId] = rate;
            }
        }

        return result;
    }

    private static OwnerRezRate? ParseRate(XDocument doc)
    {
        var lodgingRate = doc.Descendants("lodgingRate").FirstOrDefault();
        if (lodgingRate is null)
        {
            return null;
        }

        var currency = Str(lodgingRate.Element("currency")) ?? "USD";
        var nightly = lodgingRate.Element("nightlyRates");

        var perDay = WeekdayKeys
            .Select(d => Dec(nightly?.Element(d)))
            .Where(v => v is > 0)
            .Select(v => v!.Value)
            .ToList();

        long? nightlyCents = perDay.Count > 0
            ? (long)Math.Round(perDay.Average() * 100m, MidpointRounding.AwayFromZero)
            : null;

        var depositAmount =
            Dec(lodgingRate.Descendants("flatRefundableDamageDepositFees").Elements("fee").Elements("amount").FirstOrDefault())
            ?? Dec(lodgingRate.Descendants("refundableDamageDepositFlat").Elements("fee").Elements("amount").FirstOrDefault());
        long? depositCents = depositAmount is > 0
            ? (long)Math.Round(depositAmount.Value * 100m, MidpointRounding.AwayFromZero)
            : null;

        return new OwnerRezRate(nightlyCents, depositCents, currency);
    }

    /// <summary>
    /// Generic per-advertiser index reader: yields (externalId, contentUrl) for
    /// every <c>*IndexEntry</c> element (listing / rate / availability feeds).
    /// </summary>
    private static IEnumerable<(string ExternalId, string Url)> ParseIndexEntries(XDocument doc)
    {
        foreach (var entry in doc.Descendants()
            .Where(e => e.Name.LocalName.EndsWith("IndexEntry", StringComparison.Ordinal)))
        {
            var externalId = Str(entry.Elements()
                .FirstOrDefault(e => e.Name.LocalName is "listingExternalId" or "unitExternalId"));
            var url = Str(entry.Elements()
                .FirstOrDefault(e => e.Name.LocalName.EndsWith("Url", StringComparison.Ordinal)));

            if (!string.IsNullOrWhiteSpace(externalId) && !string.IsNullOrWhiteSpace(url))
            {
                yield return (externalId!, url!);
            }
        }
    }

    private static IEnumerable<(string ExternalId, string Url)> ParseBookingIndexEntries(XDocument doc)
    {
        foreach (var el in doc.Descendants()
            .Where(e => e.Name.LocalName.EndsWith("Url", StringComparison.Ordinal)))
        {
            var url = Str(el);
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            var externalId = Str(el.Parent?.Element("externalId")) ?? string.Empty;
            yield return (externalId, url!);
        }
    }

    private static List<ChannelDateBlock> ParseAvailabilityBlocks(XDocument doc)
    {
        var blocks = new List<ChannelDateBlock>();
        foreach (var el in doc.Descendants())
        {
            var name = el.Name.LocalName;
            var looksLikeRange = name.Contains("navail", StringComparison.OrdinalIgnoreCase)
                || name.Contains("block", StringComparison.OrdinalIgnoreCase)
                || name is "dateRange" or "range" or "stay";
            if (!looksLikeRange)
            {
                continue;
            }

            var begin = FirstDate(el, "beginDate", "startDate", "start", "from", "min");
            var end = FirstDate(el, "endDate", "stopDate", "end", "to", "max");
            if (begin.HasValue && end.HasValue && end.Value >= begin.Value)
            {
                var available = name.Contains("avail", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains("navail", StringComparison.OrdinalIgnoreCase);
                blocks.Add(new ChannelDateBlock(begin.Value, end.Value, available));
            }
        }

        return blocks;
    }

    // ── Booking XML builder ─────────────────────────────────────────────────

    private string BuildBookingXml(ChannelCredentials credentials, ChannelBookingPushRequest request)
    {
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency;

        var orderItems = new XElement("orderItemList");
        long totalCents = 0;
        foreach (var item in request.OrderItems)
        {
            totalCents += item.AmountCents;
            var orderItem = new XElement("orderItem",
                item.ExternalId is { Length: > 0 } extId ? new XElement("externalId", extId) : null,
                new XElement("feeType", MapFeeType(item.Type)),
                new XElement("name", item.Name),
                new XElement("preTaxAmount", new XAttribute("currency", currency), Money(item.AmountCents)),
                new XElement("status", "ACCEPTED"),
                new XElement("taxRate", "0.000000"),
                new XElement("totalAmount", new XAttribute("currency", currency), Money(item.AmountCents)));
            orderItems.Add(orderItem);
        }

        var guest = request.Guest;
        var details = new XElement("bookingRequestDetails",
            new XElement("advertiserAssignedId", credentials.ExternalAccountId),
            new XElement("listingExternalId", request.ExternalListingId),
            new XElement("unitExternalId", request.ExternalListingId),
            request.Message is { Length: > 0 } msg ? new XElement("message", msg) : null,
            new XElement("inquirer", new XAttribute("locale", "en_US"),
                new XElement("firstName", guest.FirstName),
                new XElement("lastName", guest.LastName),
                new XElement("emailAddress", guest.Email),
                guest.Phone is { Length: > 0 } phone ? new XElement("phoneNumber", phone) : null),
            request.OwnerCommissionCents is { } commission
                ? new XElement("commission",
                    new XElement("ownerFee", new XAttribute("currency", currency), Money(commission)))
                : null,
            request.GuestServiceFeeCents is { } serviceFee
                ? new XElement("olbMeta",
                    new XElement("serviceFee", new XAttribute("currency", currency), Money(serviceFee)))
                : null,
            new XElement("reservation",
                new XElement("numberOfAdults", request.Adults),
                new XElement("numberOfChildren", request.Children),
                new XElement("numberOfPets", request.Pets),
                new XElement("reservationDates",
                    new XElement("beginDate", request.CheckIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                    new XElement("endDate", request.CheckOut.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))),
                new XElement("reservationOriginationDate",
                    DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))),
            orderItems,
            new XElement("paymentScheduleItemList",
                new XElement("paymentScheduleItem",
                    new XElement("amount", new XAttribute("currency", currency), Money(totalCents)),
                    new XElement("dueDate", DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))),
            new XElement("paymentForm",
                new XElement("paymentChannelMoR",
                    new XElement("paymentFormType", "CHANNELMOR"),
                    new XElement("projectedDepositDate",
                        request.CheckIn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)))),
            new XElement("trackingUuid", request.TrackingReference),
            new XElement("travelerSource", _settings.SystemExternalId));

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("bookingRequest",
                new XElement("documentVersion", "1.3"),
                details));

        return doc.ToString(SaveOptions.DisableFormatting);
    }

    // ── HTTP plumbing ────────────────────────────────────────────────────────

    private async Task<XDocument?> GetXmlAsync(string requestUri, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient
                .GetAsync(new Uri(requestUri, UriKind.RelativeOrAbsolute), ct)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpError(logger, "GET", (int)response.StatusCode, requestUri);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(content) ? null : XDocument.Parse(content);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Xml.XmlException or TaskCanceledException)
        {
            LogRequestException(logger, $"GET {requestUri}", ex);
            return null;
        }
    }

    private async Task<XDocument?> PostXmlAsync(string requestUri, string xml, CancellationToken ct)
    {
        try
        {
            using var content = new StringContent(xml, Encoding.UTF8, "application/xml");
            using var response = await httpClient
                .PostAsync(new Uri(requestUri, UriKind.RelativeOrAbsolute), content, ct)
                .ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(body))
            {
                LogHttpError(logger, "POST", (int)response.StatusCode, requestUri);
                return null;
            }

            return string.IsNullOrWhiteSpace(body) ? null : XDocument.Parse(body);
        }
        catch (Exception ex) when (ex is HttpRequestException or System.Xml.XmlException or TaskCanceledException)
        {
            LogRequestException(logger, $"POST {requestUri}", ex);
            return null;
        }
    }

    private string WithCreds(string path)
    {
        var separator = path.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{path}{separator}type={Uri.EscapeDataString(_settings.Username)}&key={Uri.EscapeDataString(_settings.Key)}";
    }

    private bool EnsureConfigured(string method)
    {
        if (Configured)
        {
            return true;
        }

        LogNotConfigured(logger, method);
        return false;
    }

    // ── Small value parsers ──────────────────────────────────────────────────

    private static string? Str(XElement? element)
        => element?.Value.Trim() is { Length: > 0 } v ? v : null;

    private static double? Dbl(XElement? element)
        => double.TryParse(element?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static decimal? Dec(XElement? element)
        => decimal.TryParse(element?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static bool BoolOrTrue(XElement? element)
        => element is null || !bool.TryParse(element.Value, out var b) || b;

    private static string? FirstText(XElement? container)
        => container?.Descendants("textValue").FirstOrDefault()?.Value.Trim() is { Length: > 0 } v ? v : null;

    private static DateOnly? FirstDate(XElement parent, params string[] childNames)
    {
        foreach (var name in childNames)
        {
            var raw = parent.Element(name)?.Value;
            if (DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
        }

        return null;
    }

    private static DateTime? ParseDateTime(XElement? element)
        => DateTime.TryParse(element?.Value, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt) ? dt : null;

    private static string Money(long cents)
        => (cents / 100m).ToString("0.00", CultureInfo.InvariantCulture);

    private static decimal ParseBathrooms(XElement? bathrooms)
    {
        if (bathrooms is null)
        {
            return 0m;
        }

        decimal total = 0m;
        foreach (var bathroom in bathrooms.Elements("bathroom"))
        {
            var subType = Str(bathroom.Element("roomSubType"));
            total += subType is not null && subType.Contains("HALF", StringComparison.OrdinalIgnoreCase) ? 0.5m : 1m;
        }

        return total;
    }

    private static string MapPropertyType(string? ownerRezType) => (ownerRezType ?? string.Empty) switch
    {
        var s when s.Contains("APARTMENT", StringComparison.OrdinalIgnoreCase) => "apartment",
        var s when s.Contains("CONDO", StringComparison.OrdinalIgnoreCase) => "condo",
        var s when s.Contains("TOWNHOUSE", StringComparison.OrdinalIgnoreCase) => "townhouse",
        var s when s.Contains("TOWNHOME", StringComparison.OrdinalIgnoreCase) => "townhouse",
        var s when s.Contains("STUDIO", StringComparison.OrdinalIgnoreCase) => "studio",
        var s when s.Contains("LOFT", StringComparison.OrdinalIgnoreCase) => "loft",
        var s when s.Contains("VILLA", StringComparison.OrdinalIgnoreCase) => "villa",
        var s when s.Contains("COTTAGE", StringComparison.OrdinalIgnoreCase) => "cottage",
        var s when s.Contains("CABIN", StringComparison.OrdinalIgnoreCase) => "cabin",
        var s when s.Contains("HOUSE", StringComparison.OrdinalIgnoreCase) => "house",
        _ => "other",
    };

    private static string MapFeeType(string type) => type.ToUpperInvariant() switch
    {
        "RENT" or "RENTAL" => "RENTAL",
        "TAX" => "TAX",
        _ => "MISC",
    };

    private static string NormalizeBookingStatus(string status) => status.ToUpperInvariant() switch
    {
        "CONFIRMED" => "confirmed",
        "CANCELLED_BY_OWNER" or "CANCELLED_BY_TRAVELER" or "CANCELLED" => "cancelled",
        "UNCONFIRMED" => "pending",
        _ => "pending",
    };

    private static readonly string[] WeekdayKeys = ["mon", "tue", "wed", "thu", "fri", "sat", "sun"];

    private sealed record OwnerRezRate(long? NightlyCents, long? DepositCents, string Currency);

    // ── Structured logging ───────────────────────────────────────────────────

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[OwnerRez] {Method} skipped — channel credentials not configured")]
    private static partial void LogNotConfigured(ILogger logger, string method);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[OwnerRez] {Method} got HTTP {StatusCode} for {RequestUri}")]
    private static partial void LogHttpError(ILogger logger, string method, int statusCode, string requestUri);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[OwnerRez] {Operation} failed")]
    private static partial void LogRequestException(ILogger logger, string operation, Exception ex);
}
