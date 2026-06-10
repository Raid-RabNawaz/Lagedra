using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;

/// <summary>
/// Default metadata extractor. Reads only publicly declared, machine-readable
/// metadata: Open Graph tags, standard meta tags, and schema.org JSON-LD
/// (Product / Place / Accommodation / LodgingBusiness). It deliberately avoids
/// platform-specific DOM scraping, so it stays on the safe side of third-party
/// terms of service and works across many listing sites.
///
/// As a last resort it also reads the bedroom / bathroom / guest counts that
/// many platforms (Airbnb, Vrbo, Booking, …) embed verbatim in the listing
/// title or subtitle (e.g. "… · 1 bedroom · 1 bed · 1 private bath"). These are
/// public, human-readable facts already in the title text, not DOM scraping,
/// and only ever fill gaps the structured metadata left empty.
/// </summary>
public sealed partial class OpenGraphJsonLdExtractor : IListingMetadataExtractor
{
    private static readonly string[] PropertyEntityTypes =
    [
        "Accommodation", "Apartment", "House", "SingleFamilyResidence", "Residence",
        "Place", "LodgingBusiness", "Hotel", "Product", "Room", "Suite", "RentAction",
    ];

    public ImportedListingDraftDto Extract(string html, Uri finalUrl)
    {
        ArgumentNullException.ThrowIfNull(finalUrl);

        var sourceHost = NormalizeHost(finalUrl);
        if (string.IsNullOrWhiteSpace(html))
        {
            return ImportedListingDraftDto.Empty(finalUrl.ToString(), sourceHost);
        }

        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var og = ReadOpenGraph(document);
        var meta = ReadNamedMeta(document);
        var entities = ReadJsonLdEntities(document);
        var primary = SelectPrimaryEntity(entities);

        var title = FirstNonEmpty(
            og.GetValueOrDefault("og:title"),
            ReadString(primary, "name"),
            document.Title);

        var description = FirstNonEmpty(
            og.GetValueOrDefault("og:description"),
            meta.GetValueOrDefault("description"),
            ReadString(primary, "description"));

        var propertyType = ReadEntityType(primary) ?? og.GetValueOrDefault("og:type");

        var (bedrooms, bathrooms, squareFootage, maxGuests) = ReadAccommodationNumbers(primary);

        // Fill any gaps from counts the platform spells out in the title/subtitle.
        var (titleBedrooms, titleBathrooms, titleGuests) = ReadCountsFromText(
            string.Join(" \u00b7 ", new[] { title, description }.Where(s => !string.IsNullOrWhiteSpace(s))));
        bedrooms ??= titleBedrooms;
        bathrooms ??= titleBathrooms;
        maxGuests ??= titleGuests;

        var (checkIn, checkOut) = ReadCheckTimes(primary);
        var (nightlyCents, monthlyCents, currency) = ReadPricing(primary, og);
        var approxAddress = ReadApproxAddress(primary);
        var amenityHints = ReadAmenityHints(primary);
        var photos = ReadPhotos(document, og, primary, finalUrl);
        var sourceUrl = FirstNonEmpty(ReadCanonical(document), og.GetValueOrDefault("og:url"))
            ?? finalUrl.ToString();

        bool? petsAllowed = null;
        bool? smokingAllowed = null;
        bool? partiesAllowed = null;
        string? quietHoursStart = null;
        string? quietHoursEnd = null;
        string? houseRules = null;
        string? cancellationPolicy = null;

        // Airbnb-specific enrichment: og:title is auto-generated noise and the
        // real name lives in og:description, while the description, amenities,
        // photo gallery, house rules, and cancellation policy are only in the
        // page's embedded JSON state.
        if (IsAirbnb(finalUrl))
        {
            var listingName = og.GetValueOrDefault("og:description");
            if (!string.IsNullOrWhiteSpace(listingName))
            {
                title = listingName;
            }

            var state = ReadAirbnbState(document, finalUrl);
            if (!string.IsNullOrWhiteSpace(state?.Description))
            {
                description = state.Description;
            }
            else if (string.Equals(Trim(description), Trim(listingName), StringComparison.OrdinalIgnoreCase))
            {
                // Avoid the description echoing the listing name; let the host fill it.
                description = null;
            }

            if (state is { Amenities.Count: > 0 })
            {
                amenityHints = state.Amenities.ToList();
            }

            if (state is { Photos.Count: > 0 })
            {
                photos = state.Photos.ToList();
            }

            if (state is not null)
            {
                petsAllowed = state.PetsAllowed;
                smokingAllowed = state.SmokingAllowed;
                partiesAllowed = state.PartiesAllowed;
                quietHoursStart = state.QuietHoursStart;
                quietHoursEnd = state.QuietHoursEnd;
                houseRules = state.HouseRules;
                cancellationPolicy = state.CancellationPolicy;

                // House rules also expose the check-in/out times Airbnb omits from
                // structured metadata — fill them only if nothing else provided them.
                checkIn ??= state.CheckInTime;
                checkOut ??= state.CheckOutTime;
            }
        }

        title = CleanListingTitle(title);

        return new ImportedListingDraftDto(
            Title: Trim(title),
            Description: Trim(description),
            PropertyType: Trim(propertyType),
            Bedrooms: bedrooms,
            Bathrooms: bathrooms,
            SquareFootage: squareFootage,
            MaxGuests: maxGuests,
            CheckInTime: checkIn,
            CheckOutTime: checkOut,
            MonthlyRentCents: monthlyCents,
            NightlyRateCents: nightlyCents,
            Currency: currency,
            ApproxAddress: Trim(approxAddress),
            AmenityHints: amenityHints.Count > 0 ? amenityHints : null,
            Photos: photos.Count > 0 ? photos : null,
            SourceUrl: sourceUrl,
            SourceHost: sourceHost,
            PetsAllowed: petsAllowed,
            SmokingAllowed: smokingAllowed,
            PartiesAllowed: partiesAllowed,
            QuietHoursStart: quietHoursStart,
            QuietHoursEnd: quietHoursEnd,
            HouseRules: Trim(houseRules),
            CancellationPolicy: Trim(cancellationPolicy));
    }

    private static Dictionary<string, string> ReadOpenGraph(IDocument document)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in document.QuerySelectorAll("meta[property]"))
        {
            var property = element.GetAttribute("property");
            var content = element.GetAttribute("content");
            if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            // Keep the first occurrence for scalar properties. Multiple og:image
            // tags are collected separately, directly from the document.
            result.TryAdd(property, content);
        }

        return result;
    }

    private static Dictionary<string, string> ReadNamedMeta(IDocument document)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var element in document.QuerySelectorAll("meta[name]"))
        {
            var name = element.GetAttribute("name");
            var content = element.GetAttribute("content");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            result.TryAdd(name, content);
        }

        return result;
    }

    private static string? ReadCanonical(IDocument document)
    {
        var link = document.QuerySelector("link[rel=canonical]");
        var href = link?.GetAttribute("href");
        return string.IsNullOrWhiteSpace(href) ? null : href;
    }

    private static List<JsonElement> ReadJsonLdEntities(IDocument document)
    {
        var entities = new List<JsonElement>();
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
        {
            var raw = script.TextContent;
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            JsonDocument parsed;
            try
            {
                parsed = JsonDocument.Parse(raw);
            }
            catch (JsonException)
            {
                continue;
            }

            FlattenJsonLd(parsed.RootElement, entities);
        }

        return entities;
    }

    private static void FlattenJsonLd(JsonElement element, List<JsonElement> sink)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    FlattenJsonLd(item, sink);
                }

                break;

            case JsonValueKind.Object:
                if (element.TryGetProperty("@graph", out var graph) &&
                    graph.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in graph.EnumerateArray())
                    {
                        FlattenJsonLd(item, sink);
                    }
                }

                sink.Add(element.Clone());
                break;

            default:
                break;
        }
    }

    private static JsonElement? SelectPrimaryEntity(List<JsonElement> entities)
    {
        // Prefer an entity whose @type looks like a property/place/product.
        foreach (var entity in entities)
        {
            var type = ReadEntityType(entity);
            if (type is not null &&
                PropertyEntityTypes.Any(t => type.Contains(t, StringComparison.OrdinalIgnoreCase)))
            {
                return entity;
            }
        }

        // Otherwise fall back to the first entity that at least has a name.
        foreach (var entity in entities)
        {
            if (ReadString(entity, "name") is not null)
            {
                return entity;
            }
        }

        return entities.Count > 0 ? entities[0] : null;
    }

    private static string? ReadEntityType(JsonElement? entity)
    {
        if (entity is not { } e || e.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!e.TryGetProperty("@type", out var type))
        {
            return null;
        }

        return type.ValueKind switch
        {
            JsonValueKind.String => type.GetString(),
            JsonValueKind.Array => type.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .FirstOrDefault(),
            _ => null,
        };
    }

    private static (int? Bedrooms, decimal? Bathrooms, int? SquareFootage, int? MaxGuests)
        ReadAccommodationNumbers(JsonElement? entity)
    {
        if (entity is not { } e || e.ValueKind != JsonValueKind.Object)
        {
            return (null, null, null, null);
        }

        var bedrooms = ReadInt(e, "numberOfBedrooms") ?? ReadInt(e, "numberOfRooms");
        var bathrooms = ReadDecimal(e, "numberOfBathroomsTotal") ?? ReadDecimal(e, "numberOfBathrooms");
        var maxGuests = ReadInt(e, "occupancy") ?? ReadOccupancy(e);

        int? squareFootage = null;
        if (e.TryGetProperty("floorSize", out var floor))
        {
            squareFootage = floor.ValueKind == JsonValueKind.Object
                ? ToInt(ReadDecimal(floor, "value"))
                : ToInt(ParseDecimal(GetScalarString(floor)));
        }

        return (bedrooms, bathrooms, squareFootage, maxGuests);
    }

    /// <summary>
    /// Parses bedroom / bathroom / guest counts that listing platforms embed in
    /// the title or subtitle, e.g. "Tiny home in Chapel Hill · 1 bedroom · 1 bed ·
    /// 1 private bath" or "4 guests · 2 bedrooms · 1.5 baths". A "Studio" with no
    /// explicit bedroom count is treated as zero bedrooms.
    /// </summary>
    private static (int? Bedrooms, decimal? Bathrooms, int? MaxGuests) ReadCountsFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (null, null, null);
        }

        int? bedrooms = null;
        var bedroomMatch = BedroomCountRegex().Match(text);
        if (bedroomMatch.Success &&
            int.TryParse(bedroomMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var beds))
        {
            bedrooms = beds;
        }
        else if (StudioRegex().IsMatch(text))
        {
            bedrooms = 0;
        }

        decimal? bathrooms = null;
        var bathMatch = BathCountRegex().Match(text);
        if (bathMatch.Success &&
            decimal.TryParse(bathMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var baths))
        {
            bathrooms = baths;
        }

        int? guests = null;
        var guestMatch = GuestCountRegex().Match(text);
        if (guestMatch.Success &&
            int.TryParse(guestMatch.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var g))
        {
            guests = g;
        }

        return (bedrooms, bathrooms, guests);
    }

    [GeneratedRegex(@"(\d+)\s*bedrooms?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BedroomCountRegex();

    [GeneratedRegex(@"\bstudio\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex StudioRegex();

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(?:private\s+|shared\s+|full\s+|half\s+)?baths?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BathCountRegex();

    [GeneratedRegex(@"(\d+)\s*guests?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GuestCountRegex();

    private static int? ReadOccupancy(JsonElement entity)
    {
        if (!entity.TryGetProperty("occupancy", out var occ))
        {
            return null;
        }

        if (occ.ValueKind == JsonValueKind.Object)
        {
            return ReadInt(occ, "maxValue") ?? ReadInt(occ, "value");
        }

        return ToInt(ParseDecimal(GetScalarString(occ)));
    }

    private static (string? CheckIn, string? CheckOut) ReadCheckTimes(JsonElement? entity)
    {
        if (entity is not { } e || e.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        return (NormalizeTime(ReadString(e, "checkinTime")), NormalizeTime(ReadString(e, "checkoutTime")));
    }

    private static (long? NightlyCents, long? MonthlyCents, string? Currency)
        ReadPricing(JsonElement? entity, Dictionary<string, string> og)
    {
        decimal? amount = null;
        string? currency = null;
        var isMonthly = false;

        if (entity is { } e && e.ValueKind == JsonValueKind.Object &&
            e.TryGetProperty("offers", out var offers))
        {
            var offer = offers.ValueKind == JsonValueKind.Array
                ? offers.EnumerateArray().FirstOrDefault()
                : offers;

            if (offer.ValueKind == JsonValueKind.Object)
            {
                amount = ReadDecimal(offer, "price");
                currency = ReadString(offer, "priceCurrency");

                if (amount is null && offer.TryGetProperty("priceSpecification", out var spec) &&
                    spec.ValueKind == JsonValueKind.Object)
                {
                    amount = ReadDecimal(spec, "price");
                    currency ??= ReadString(spec, "priceCurrency");
                    isMonthly = MentionsMonth(
                        ReadString(spec, "unitText") ??
                        ReadString(spec, "unitCode") ??
                        ReadString(spec, "billingDuration"));
                }
            }
        }

        if (amount is null && og.TryGetValue("og:price:amount", out var ogAmount))
        {
            amount = ParseDecimal(ogAmount);
            currency ??= og.GetValueOrDefault("og:price:currency");
        }

        if (amount is null && og.TryGetValue("product:price:amount", out var prodAmount))
        {
            amount = ParseDecimal(prodAmount);
            currency ??= og.GetValueOrDefault("product:price:currency");
        }

        currency = NormalizeCurrency(currency);

        if (amount is not { } value || value <= 0)
        {
            return (null, null, currency);
        }

        var cents = (long)Math.Round(value * 100m, MidpointRounding.AwayFromZero);
        return isMonthly ? (null, cents, currency) : (cents, null, currency);
    }

    private static string? ReadApproxAddress(JsonElement? entity)
    {
        if (entity is not { } e || e.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!e.TryGetProperty("address", out var address))
        {
            return null;
        }

        if (address.ValueKind == JsonValueKind.String)
        {
            // Free-form string: keep it but never expand precise GPS.
            return address.GetString();
        }

        if (address.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        // City/region only — deliberately omit streetAddress and postal code.
        var city = ReadString(address, "addressLocality");
        var region = ReadString(address, "addressRegion");
        var country = ReadString(address, "addressCountry");

        var parts = new[] { city, region, country }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        return parts.Length > 0 ? string.Join(", ", parts) : null;
    }

    private static List<string> ReadAmenityHints(JsonElement? entity)
    {
        var hints = new List<string>();
        if (entity is not { } e || e.ValueKind != JsonValueKind.Object)
        {
            return hints;
        }

        if (!e.TryGetProperty("amenityFeature", out var features))
        {
            return hints;
        }

        var items = features.ValueKind == JsonValueKind.Array
            ? features.EnumerateArray()
            : new[] { features }.AsEnumerable();

        foreach (var feature in items)
        {
            if (feature.ValueKind == JsonValueKind.String)
            {
                AddHint(hints, feature.GetString());
                continue;
            }

            if (feature.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            // Only surface features that are present/true.
            if (feature.TryGetProperty("value", out var value) &&
                value.ValueKind == JsonValueKind.False)
            {
                continue;
            }

            AddHint(hints, ReadString(feature, "name"));
        }

        return hints;
    }

    private static List<ImportedPhotoCandidateDto> ReadPhotos(
        IDocument document,
        Dictionary<string, string> og,
        JsonElement? entity,
        Uri finalUrl)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var photos = new List<ImportedPhotoCandidateDto>();

        var ogAlt = og.GetValueOrDefault("og:image:alt");
        int? ogWidth = ParseInt(og.GetValueOrDefault("og:image:width"));
        int? ogHeight = ParseInt(og.GetValueOrDefault("og:image:height"));

        // Collect every og:image / og:image:url / og:image:secure_url occurrence
        // in document order.
        foreach (var element in document.QuerySelectorAll(
            "meta[property='og:image'], meta[property='og:image:url'], meta[property='og:image:secure_url']"))
        {
            TryAddPhoto(photos, seen, element.GetAttribute("content"), ogAlt, ogWidth, ogHeight, finalUrl);
        }

        if (entity is { } e && e.ValueKind == JsonValueKind.Object &&
            e.TryGetProperty("image", out var image))
        {
            CollectJsonLdImages(image, photos, seen, finalUrl);
        }

        return photos;
    }

    private static void CollectJsonLdImages(
        JsonElement image,
        List<ImportedPhotoCandidateDto> photos,
        HashSet<string> seen,
        Uri finalUrl)
    {
        switch (image.ValueKind)
        {
            case JsonValueKind.String:
                TryAddPhoto(photos, seen, image.GetString(), null, null, null, finalUrl);
                break;

            case JsonValueKind.Array:
                foreach (var item in image.EnumerateArray())
                {
                    CollectJsonLdImages(item, photos, seen, finalUrl);
                }

                break;

            case JsonValueKind.Object:
                TryAddPhoto(
                    photos,
                    seen,
                    ReadString(image, "url") ?? ReadString(image, "contentUrl"),
                    ReadString(image, "caption") ?? ReadString(image, "name"),
                    ToInt(ReadDecimal(image, "width")),
                    ToInt(ReadDecimal(image, "height")),
                    finalUrl);
                break;

            default:
                break;
        }
    }

    private static void TryAddPhoto(
        List<ImportedPhotoCandidateDto> photos,
        HashSet<string> seen,
        string? url,
        string? alt,
        int? width,
        int? height,
        Uri finalUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (!Uri.TryCreate(finalUrl, url.Trim(), out var absolute))
        {
            return;
        }

        if (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps)
        {
            return;
        }

        var normalized = absolute.ToString();
        if (!seen.Add(normalized))
        {
            return;
        }

        photos.Add(new ImportedPhotoCandidateDto(
            normalized,
            string.IsNullOrWhiteSpace(alt) ? null : alt.Trim(),
            width,
            height));
    }

    // ── small helpers ──────────────────────────────────────────────

    private static void AddHint(List<string> hints, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (!hints.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
        {
            hints.Add(trimmed);
        }
    }

    private static string NormalizeHost(Uri uri)
    {
        var host = uri.Host;
        return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ReadString(JsonElement? entity, string property)
    {
        if (entity is not { } e || e.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!e.TryGetProperty(property, out var value))
        {
            return null;
        }

        var scalar = GetScalarString(value);
        return string.IsNullOrWhiteSpace(scalar) ? null : scalar;
    }

    private static string? GetScalarString(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => null,
    };

    private static int? ReadInt(JsonElement entity, string property) =>
        ToInt(ReadDecimal(entity, property));

    private static decimal? ReadDecimal(JsonElement entity, string property)
    {
        if (entity.ValueKind != JsonValueKind.Object || !entity.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var d) => d,
            JsonValueKind.String => ParseDecimal(value.GetString()),
            _ => null,
        };
    }

    private static int? ToInt(decimal? value) =>
        value is { } v ? (int)Math.Round(v, MidpointRounding.AwayFromZero) : null;

    private static int? ParseInt(string? value) => ToInt(ParseDecimal(value));

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = new string(value.Where(c => char.IsDigit(c) || c is '.' or ',' or '-').ToArray());
        if (cleaned.Length == 0)
        {
            return null;
        }

        // Drop thousands separators; assume '.' is the decimal point.
        cleaned = cleaned.Replace(",", string.Empty, StringComparison.Ordinal);
        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static bool MentionsMonth(string? unit) =>
        !string.IsNullOrWhiteSpace(unit) &&
        (unit.Contains("month", StringComparison.OrdinalIgnoreCase) ||
         unit.Equals("MON", StringComparison.OrdinalIgnoreCase));

    private static string? NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return null;
        }

        var value = currency.Trim();
        if (value.Length == 3 && value.All(char.IsLetter))
        {
            return value.ToUpperInvariant();
        }

        return value switch
        {
            "$" => "USD",
            "€" => "EUR",
            "£" => "GBP",
            _ => null,
        };
    }

    private static string? NormalizeTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, out var time))
        {
            return time.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        return null;
    }
}
