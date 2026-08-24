using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;

/// <summary>
/// Airbnb-aware enrichment for <see cref="OpenGraphJsonLdExtractor"/>.
///
/// Airbnb publishes almost nothing useful in Open Graph / JSON-LD: the og:title
/// is an auto-generated "Room type in City · ★rating · N bedroom · N bed · N bath"
/// string, og:description holds the host's actual listing name, and there is no
/// machine-readable description, amenity list, photo gallery, house rules, or
/// cancellation policy. All of that data is, however, present (publicly, for a
/// listing the host owns) inside the page's embedded GraphQL bootstrap state — a
/// single <c>&lt;script id="data-deferred-state-0" type="application/json"&gt;</c> blob.
///
/// This reader walks that JSON defensively, anchoring only on stable field names
/// (<c>htmlDescription.htmlText</c>, <c>AmenityItem</c>, listing-scoped
/// <c>.../im/pictures/.../Hosting-&lt;id&gt;/...</c> photo URLs, <c>houseRulesSections</c>,
/// and <c>localized_cancellation_policy_name</c>). It never throws: anything it
/// cannot find is simply left for the generic extractor to fill.
/// </summary>
public sealed partial class OpenGraphJsonLdExtractor
{
    /// <summary>Upper bound for the description column (see ListingConfiguration).</summary>
    private const int DescriptionMaxLength = 4900;

    /// <summary>Guards against pathologically deep JSON while walking the state.</summary>
    private const int MaxWalkDepth = 96;

    internal sealed record AirbnbListingState(
        string? Description,
        IReadOnlyList<string> Amenities,
        IReadOnlyList<ImportedPhotoCandidateDto> Photos,
        bool? PetsAllowed,
        bool? SmokingAllowed,
        bool? PartiesAllowed,
        string? QuietHoursStart,
        string? QuietHoursEnd,
        string? CheckInTime,
        string? CheckOutTime,
        string? HouseRules,
        string? CancellationPolicy);

    private sealed class AirbnbStateAccumulator
    {
        public string? Description { get; set; }

        public List<string> Amenities { get; } = [];

        public HashSet<string> AmenitySeen { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<ImportedPhotoCandidateDto> Photos { get; } = [];

        public HashSet<string> PhotoSeen { get; } = new(StringComparer.OrdinalIgnoreCase);

        public bool? PetsAllowed { get; set; }

        public bool? SmokingAllowed { get; set; }

        public bool? PartiesAllowed { get; set; }

        public string? QuietHoursStart { get; set; }

        public string? QuietHoursEnd { get; set; }

        public string? CheckInTime { get; set; }

        public string? CheckOutTime { get; set; }

        public string? HouseRules { get; set; }

        public string? CancellationPolicy { get; set; }
    }

    private static bool IsAirbnb(Uri url) =>
        url.Host.Contains("airbnb.", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads the listing description, amenities, photo gallery, house rules, and
    /// cancellation policy from the Airbnb page's embedded JSON state. Returns
    /// <c>null</c> when nothing usable could be found.
    /// </summary>
    private static AirbnbListingState? ReadAirbnbState(IDocument document, Uri finalUrl)
    {
        var roomIdMatch = AirbnbRoomIdRegex().Match(finalUrl.AbsolutePath);
        var listingId = roomIdMatch.Success ? roomIdMatch.Groups[1].Value : null;

        var acc = new AirbnbStateAccumulator();

        foreach (var script in document.QuerySelectorAll("script[type='application/json']"))
        {
            var raw = script.TextContent;
            if (string.IsNullOrWhiteSpace(raw) || raw.Length < 200)
            {
                continue;
            }

            // Only parse scripts that actually look like PDP state.
            if (!raw.Contains("/im/pictures/", StringComparison.OrdinalIgnoreCase) &&
                !raw.Contains("AmenityItem", StringComparison.Ordinal) &&
                !raw.Contains("htmlDescription", StringComparison.Ordinal) &&
                !raw.Contains("houseRulesSections", StringComparison.Ordinal))
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

            using (parsed)
            {
                WalkAirbnbNode(parsed.RootElement, listingId, 0, acc);
            }
        }

        var description = CleanDescriptionHtml(acc.Description);
        var hasAnything = description is not null ||
            acc.Amenities.Count > 0 ||
            acc.Photos.Count > 0 ||
            acc.PetsAllowed is not null ||
            acc.SmokingAllowed is not null ||
            acc.PartiesAllowed is not null ||
            acc.QuietHoursStart is not null ||
            acc.HouseRules is not null ||
            acc.CancellationPolicy is not null;

        if (!hasAnything)
        {
            return null;
        }

        return new AirbnbListingState(
            description,
            acc.Amenities,
            acc.Photos,
            acc.PetsAllowed,
            acc.SmokingAllowed,
            acc.PartiesAllowed,
            acc.QuietHoursStart,
            acc.QuietHoursEnd,
            acc.CheckInTime,
            acc.CheckOutTime,
            CleanDescriptionHtml(acc.HouseRules),
            acc.CancellationPolicy);
    }

    private static void WalkAirbnbNode(
        JsonElement element,
        string? listingId,
        int depth,
        AirbnbStateAccumulator acc)
    {
        if (depth > MaxWalkDepth)
        {
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                CollectAirbnbAmenity(element, acc);
                CollectAirbnbDescription(element, acc);
                CollectAirbnbHouseRules(element, acc);
                CollectAirbnbCancellation(element, acc);

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        TryAddAirbnbPhoto(property.Value.GetString(), listingId, acc);
                    }
                    else
                    {
                        WalkAirbnbNode(property.Value, listingId, depth + 1, acc);
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        TryAddAirbnbPhoto(item.GetString(), listingId, acc);
                    }
                    else
                    {
                        WalkAirbnbNode(item, listingId, depth + 1, acc);
                    }
                }

                break;

            default:
                break;
        }
    }

    private static void CollectAirbnbAmenity(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (!string.Equals(ReadString(element, "__typename"), "AmenityItem", StringComparison.Ordinal))
        {
            return;
        }

        // Skip amenities the listing explicitly marks as unavailable.
        if (element.TryGetProperty("available", out var available) &&
            available.ValueKind == JsonValueKind.False)
        {
            return;
        }

        var title = ReadString(element, "title");
        if (!string.IsNullOrWhiteSpace(title) && acc.AmenitySeen.Add(title.Trim()))
        {
            acc.Amenities.Add(title.Trim());
        }
    }

    private static void CollectAirbnbDescription(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (!element.TryGetProperty("htmlDescription", out var htmlDescription) ||
            htmlDescription.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var htmlText = ReadString(htmlDescription, "htmlText");
        if (string.IsNullOrWhiteSpace(htmlText))
        {
            return;
        }

        // Keep the richest description if several sections expose htmlText.
        if (acc.Description is null || htmlText.Length > acc.Description.Length)
        {
            acc.Description = htmlText;
        }
    }

    private static void CollectAirbnbCancellation(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (acc.CancellationPolicy is not null)
        {
            return;
        }

        var name = ReadString(element, "localized_cancellation_policy_name");
        if (!string.IsNullOrWhiteSpace(name))
        {
            acc.CancellationPolicy = name.Trim();
        }
    }

    /// <summary>
    /// Reads Airbnb's structured "House rules" list (check-in/out, guest cap,
    /// pets/smoking/parties, quiet hours, and the host's free-text additional
    /// rules) from a <c>houseRulesSections</c> node.
    /// </summary>
    private static void CollectAirbnbHouseRules(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (!element.TryGetProperty("houseRulesSections", out var sections) ||
            sections.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var section in sections.EnumerateArray())
        {
            if (section.ValueKind != JsonValueKind.Object ||
                !section.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var item in items.EnumerateArray())
            {
                InterpretHouseRuleItem(item, acc);
            }
        }
    }

    private static void InterpretHouseRuleItem(JsonElement item, AirbnbStateAccumulator acc)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var title = NormalizeSpaces(ReadString(item, "title"));
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        if (Has(title, "no pets"))
        {
            acc.PetsAllowed = false;
        }
        else if (Has(title, "pets allowed") || Has(title, "pet allowed"))
        {
            acc.PetsAllowed = true;
        }

        if (Has(title, "no smoking"))
        {
            acc.SmokingAllowed = false;
        }
        else if (Has(title, "smoking allowed"))
        {
            acc.SmokingAllowed = true;
        }

        if (Has(title, "no parties") || Has(title, "no events"))
        {
            acc.PartiesAllowed = false;
        }
        else if (Has(title, "parties allowed") || Has(title, "events allowed"))
        {
            acc.PartiesAllowed = true;
        }

        if (Has(title, "check-in") || Has(title, "check in"))
        {
            acc.CheckInTime ??= ParseClockTime(title);
        }
        else if (Has(title, "checkout") || Has(title, "check-out") || Has(title, "check out"))
        {
            acc.CheckOutTime ??= ParseClockTime(title);
        }

        if (Has(title, "quiet hours"))
        {
            var subtitle = NormalizeSpaces(ReadString(item, "subtitle"));
            var times = ClockTimeRegex().Matches(subtitle ?? string.Empty);
            if (times.Count >= 2)
            {
                acc.QuietHoursStart ??= NormalizeClock(times[0].Value);
                acc.QuietHoursEnd ??= NormalizeClock(times[1].Value);
            }
        }

        if (Has(title, "additional rules") &&
            item.TryGetProperty("html", out var html) &&
            html.ValueKind == JsonValueKind.Object)
        {
            var htmlText = ReadString(html, "htmlText");
            if (!string.IsNullOrWhiteSpace(htmlText))
            {
                acc.HouseRules ??= htmlText;
            }
        }
    }

    /// <summary>
    /// Cap how many Airbnb gallery photos we surface. Listings often expose
    /// 40–80 CDN URLs; importing all of them makes create/edit timeouts and
    /// saturates storage. Hosts can still upload more from the photo editor.
    /// </summary>
    private const int MaxImportedPhotos = 20;

    private static void TryAddAirbnbPhoto(string? value, string? listingId, AirbnbStateAccumulator acc)
    {
        if (acc.Photos.Count >= MaxImportedPhotos)
        {
            return;
        }

        if (string.IsNullOrEmpty(value) ||
            !value.Contains("/im/pictures/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Only listing photos (scoped to this listing's "Hosting-<id>" namespace),
        // never platform assets (favicons, icons, host avatars, map tiles).
        var marker = listingId is null ? "Hosting-" : "Hosting-" + listingId;
        if (!value.Contains(marker, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        // Dedupe by path so the same photo at different sizes counts once, then
        // request a sensibly sized render rather than the multi-MB original.
        var path = uri.GetLeftPart(UriPartial.Path);
        if (!acc.PhotoSeen.Add(path))
        {
            return;
        }

        acc.Photos.Add(new ImportedPhotoCandidateDto($"{path}?im_w=1200"));
    }

    /// <summary>
    /// Turns an HTML fragment (with &lt;br&gt; line breaks and the occasional
    /// anchor tag) into clean plain text, capped to the column size.
    /// </summary>
    private static string? CleanDescriptionHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var text = BrTagRegex().Replace(html, "\n");
        text = HtmlTagRegex().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        text = ExcessNewlineRegex().Replace(text, "\n\n").Trim();

        if (text.Length > DescriptionMaxLength)
        {
            text = text[..DescriptionMaxLength].TrimEnd();
        }

        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>
    /// Strips the auto-generated noise platforms append to listing titles, e.g.
    /// the "★4.93" rating and trailing "· 1 bedroom · 1 bed · 1 private bath"
    /// specification segments, leaving the human-readable name intact.
    /// </summary>
    private static string? CleanListingTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Trim(title);
        }

        var working = StarRatingRegex().Replace(title, " ");

        var segments = working.Split(
            ['\u00b7', '\u2022', '\u2219', '|', '\u2013', '\u2014'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var kept = segments.Where(s => !SpecSegmentRegex().IsMatch(s)).ToList();
        var result = (kept.Count > 0 ? string.Join(" \u00b7 ", kept) : working).Trim();
        result = ExcessSpaceRegex().Replace(result, " ").Trim(' ', '\u00b7', '-', ',');

        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    private static bool Has(string text, string token) =>
        text.Contains(token, StringComparison.OrdinalIgnoreCase);

    /// <summary>Normalizes non-breaking / thin spaces to a regular space.</summary>
    private static string? NormalizeSpaces(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var cleaned = value
            .Replace('\u00a0', ' ')
            .Replace('\u202f', ' ')
            .Replace('\u2009', ' ')
            .Replace('\u2007', ' ');
        return ExcessSpaceRegex().Replace(cleaned, " ").Trim();
    }

    /// <summary>Pulls the first "h[:mm] AM/PM" or 24h time from text as "HH:mm".</summary>
    private static string? ParseClockTime(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var match = ClockTimeRegex().Match(text);
        return match.Success ? NormalizeClock(match.Value) : null;
    }

    private static string? NormalizeClock(string token)
    {
        var value = NormalizeSpaces(token)!;
        string[] formats = ["h:mm tt", "htt", "h tt", "hh:mm tt", "HH:mm", "H:mm"];
        if (DateTime.TryParseExact(
                value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ||
            DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return parsed.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        return null;
    }

    [GeneratedRegex(@"/rooms/(?:plus/)?(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AirbnbRoomIdRegex();

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BrTagRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.CultureInvariant)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\n{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex ExcessNewlineRegex();

    [GeneratedRegex(@"\s{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex ExcessSpaceRegex();

    [GeneratedRegex(@"★\s*\d+(?:[.,]\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex StarRatingRegex();

    [GeneratedRegex(
        @"^\d+(?:\.\d+)?\s*(?:bedrooms?|beds?|(?:private\s+|shared\s+|full\s+|half\s+)?baths?|bathrooms?|guests?|sq\s*\.?\s*ft)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SpecSegmentRegex();

    [GeneratedRegex(@"\d{1,2}:\d{2}\s*(?:[AP]M)?|\d{1,2}\s*[AP]M",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ClockTimeRegex();
}
