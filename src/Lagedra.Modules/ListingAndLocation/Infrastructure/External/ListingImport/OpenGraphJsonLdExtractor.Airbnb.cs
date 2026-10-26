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
/// Airbnb publishes little that is useful in Open Graph: og:title / og:description
/// are the generic site title and an empty string on current pages. Its schema.org
/// JSON-LD (<c>VacationRental</c>) carries the name, a truncated description, a
/// handful of photos, occupancy and the city — the generic extractor reads those.
/// Everything else (full description, amenity list, complete photo gallery, house
/// rules, bedroom/bath/guest counts, property type) lives in the page's embedded
/// GraphQL bootstrap state — a <c>&lt;script id="data-deferred-state-0"
/// type="application/json"&gt;</c> blob that is present in the server-rendered HTML.
///
/// This reader walks that JSON defensively and anchors only on typed nodes that
/// have been stable across listings of different vintages (verified Oct 2026):
/// <list type="bullet">
///   <item><c>PdpUGCText</c> / <c>UGCText</c> for the listing name and
///   <c>longDescriptionHtml</c> for the description;</item>
///   <item><c>StaysPdpOverview</c> for "N guests · N bedrooms · N baths";</item>
///   <item><c>AmenityItem</c> for amenities;</item>
///   <item><c>HeroImageItem</c> / <c>ImageMediaTourItem</c> /
///   <c>PhotoTourModalSection.mediaItems</c> for the gallery;</item>
///   <item><c>StaysPdpDetailItem</c> with a <c>HOUSE_RULES_*</c> type for rules.</item>
/// </list>
/// The pre-2026 shapes (<c>htmlDescription.htmlText</c>, <c>houseRulesSections</c>,
/// <c>Hosting-&lt;id&gt;</c> photo paths, <c>localized_cancellation_policy_name</c>)
/// are still recognised. It never throws: anything it cannot find is simply left
/// for the generic extractor to fill.
///
/// Note: the cancellation policy is not part of the server-rendered state (Airbnb
/// only resolves it once dates are chosen), so it is usually unavailable.
/// </summary>
public sealed partial class OpenGraphJsonLdExtractor
{
    /// <summary>Upper bound for the description column (see ListingConfiguration).</summary>
    private const int DescriptionMaxLength = 4900;

    /// <summary>Guards against pathologically deep JSON while walking the state.</summary>
    private const int MaxWalkDepth = 96;

    internal sealed record AirbnbListingState(
        string? Name,
        string? Description,
        string? PropertyType,
        string? Location,
        string? OverviewText,
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
        public string? Name { get; set; }

        public string? Description { get; set; }

        public string? PropertyType { get; set; }

        public string? Location { get; set; }

        public string? OverviewTitle { get; set; }

        public string? OverviewText { get; set; }

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

        /// <summary>Host-written rule paragraphs, in page order (rules, then requests).</summary>
        public List<string> HouseRuleParagraphs { get; } = [];

        public string? CancellationPolicy { get; set; }
    }

    private static bool IsAirbnb(Uri url) =>
        url.Host.Contains("airbnb.", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads the listing name, description, overview counts, amenities, photo
    /// gallery, house rules, and (when present) cancellation policy from the
    /// Airbnb page's embedded JSON state. Returns <c>null</c> when nothing usable
    /// could be found.
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
                !raw.Contains("longDescriptionHtml", StringComparison.Ordinal) &&
                !raw.Contains("HOUSE_RULES_", StringComparison.Ordinal) &&
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
        var houseRules = acc.HouseRuleParagraphs.Count == 0
            ? null
            : CleanDescriptionHtml(string.Join("\n\n", acc.HouseRuleParagraphs));
        var location = acc.Location ?? LocationFromOverviewTitle(acc.OverviewTitle);

        var hasAnything = acc.Name is not null ||
            description is not null ||
            acc.PropertyType is not null ||
            location is not null ||
            acc.OverviewText is not null ||
            acc.Amenities.Count > 0 ||
            acc.Photos.Count > 0 ||
            acc.PetsAllowed is not null ||
            acc.SmokingAllowed is not null ||
            acc.PartiesAllowed is not null ||
            acc.QuietHoursStart is not null ||
            acc.CheckInTime is not null ||
            acc.CheckOutTime is not null ||
            houseRules is not null ||
            acc.CancellationPolicy is not null;

        if (!hasAnything)
        {
            return null;
        }

        return new AirbnbListingState(
            acc.Name,
            description,
            acc.PropertyType,
            location,
            acc.OverviewText,
            acc.Amenities,
            acc.Photos,
            acc.PetsAllowed,
            acc.SmokingAllowed,
            acc.PartiesAllowed,
            acc.QuietHoursStart,
            acc.QuietHoursEnd,
            acc.CheckInTime,
            acc.CheckOutTime,
            houseRules,
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
                CollectAirbnbListingFacts(element, acc);
                CollectAirbnbName(element, acc);
                CollectAirbnbDescription(element, acc);
                CollectAirbnbOverview(element, acc);
                CollectAirbnbLocation(element, acc);
                CollectAirbnbAmenity(element, acc);
                CollectAirbnbGalleryPhotos(element, acc);
                CollectAirbnbHouseRules(element, acc);
                CollectAirbnbCancellation(element, acc);

                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        TryAddAirbnbPhoto(property.Value.GetString(), listingId, requireListingMarker: true, acc);
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
                        TryAddAirbnbPhoto(item.GetString(), listingId, requireListingMarker: true, acc);
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

    // ── listing node: property type ─────────────────────────────────

    /// <summary>
    /// The <c>DemandStayListing</c> node carries <c>propertyType</c> ("APARTMENT",
    /// "CONDOMINIUM", "PRIVATE_SUITE", …) and <c>spaceType</c> ("ENTIRE_HOME", …).
    /// Both must be present so that an unrelated <c>propertyType</c> key elsewhere
    /// in the state is not mistaken for the listing's.
    /// </summary>
    private static void CollectAirbnbListingFacts(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (acc.PropertyType is not null ||
            !element.TryGetProperty("spaceType", out var spaceType) ||
            spaceType.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var propertyType = ReadString(element, "propertyType");
        if (!string.IsNullOrWhiteSpace(propertyType))
        {
            acc.PropertyType = HumanizeEnumToken(propertyType);
        }
    }

    // ── name ────────────────────────────────────────────────────────

    /// <summary>
    /// The host's listing name is the <c>PdpUGCText</c> under
    /// <c>pdpPresentation.title</c>, and also <c>DemandStayListingDescription.name</c>.
    /// </summary>
    private static void CollectAirbnbName(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (acc.Name is not null)
        {
            return;
        }

        if (element.TryGetProperty("title", out var title) &&
            title.ValueKind == JsonValueKind.Object &&
            string.Equals(ReadString(title, "__typename"), "PdpUGCText", StringComparison.Ordinal))
        {
            acc.Name = NormalizeSpaces(ReadUgcText(title));
            return;
        }

        if (string.Equals(ReadString(element, "__typename"), "DemandStayListingDescription", StringComparison.Ordinal) &&
            element.TryGetProperty("name", out var name))
        {
            acc.Name = NormalizeSpaces(ReadUgcText(name));
        }
    }

    // ── description ─────────────────────────────────────────────────

    private static void CollectAirbnbDescription(JsonElement element, AirbnbStateAccumulator acc)
    {
        // Current shape: pdpPresentation.descriptions.longDescriptionHtml (UGCText),
        // with shortDescriptionHtml (PdpUGCText) as a truncated sibling.
        if (element.TryGetProperty("longDescriptionHtml", out var longHtml))
        {
            OfferDescription(ReadUgcText(longHtml), acc);
        }

        if (element.TryGetProperty("shortDescriptionHtml", out var shortHtml))
        {
            OfferDescription(ReadUgcText(shortHtml), acc);
        }

        // Legacy shape: { "htmlDescription": { "htmlText": "…" } }.
        if (element.TryGetProperty("htmlDescription", out var htmlDescription) &&
            htmlDescription.ValueKind == JsonValueKind.Object)
        {
            OfferDescription(ReadString(htmlDescription, "htmlText"), acc);
        }
    }

    /// <summary>Keeps the richest description if several nodes expose one.</summary>
    private static void OfferDescription(string? candidate, AirbnbStateAccumulator acc)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return;
        }

        if (acc.Description is null || candidate.Length > acc.Description.Length)
        {
            acc.Description = candidate;
        }
    }

    // ── overview / location ─────────────────────────────────────────

    /// <summary>
    /// <c>StaysPdpOverview</c> is the "Entire rental unit in Gray, Tennessee" header
    /// with items like ["3 guests", "2 bedrooms", "2 beds", "1 bath"]. The items feed
    /// the generic count parser; the title yields an approximate location.
    /// </summary>
    private static void CollectAirbnbOverview(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (acc.OverviewText is not null ||
            !string.Equals(ReadString(element, "__typename"), "StaysPdpOverview", StringComparison.Ordinal))
        {
            return;
        }

        acc.OverviewTitle = NormalizeSpaces(ReadString(element, "title"));

        if (element.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            var parts = items.EnumerateArray()
                .Where(i => i.ValueKind == JsonValueKind.String)
                .Select(i => NormalizeSpaces(i.GetString()))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (parts.Count > 0)
            {
                acc.OverviewText = string.Join(" \u00b7 ", parts);
            }
        }
    }

    /// <summary><c>StaysPdpLocation.subtitle</c> is "City, Region, Country" (no street).</summary>
    private static void CollectAirbnbLocation(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (acc.Location is not null ||
            !string.Equals(ReadString(element, "__typename"), "StaysPdpLocation", StringComparison.Ordinal))
        {
            return;
        }

        acc.Location = NormalizeSpaces(ReadString(element, "subtitle"));
    }

    /// <summary>"Entire rental unit in Gray, Tennessee, United States" → "Gray, Tennessee, United States".</summary>
    private static string? LocationFromOverviewTitle(string? overviewTitle)
    {
        if (string.IsNullOrWhiteSpace(overviewTitle))
        {
            return null;
        }

        var index = overviewTitle.LastIndexOf(" in ", StringComparison.OrdinalIgnoreCase);
        if (index < 0 || index + 4 >= overviewTitle.Length)
        {
            return null;
        }

        var location = overviewTitle[(index + 4)..].Trim();
        return string.IsNullOrWhiteSpace(location) ? null : location;
    }

    // ── amenities ───────────────────────────────────────────────────

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

    // ── cancellation policy ─────────────────────────────────────────

    private static void CollectAirbnbCancellation(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (acc.CancellationPolicy is not null)
        {
            return;
        }

        var name = ReadString(element, "localized_cancellation_policy_name") ??
            ReadString(element, "localizedCancellationPolicyName");
        if (!string.IsNullOrWhiteSpace(name))
        {
            acc.CancellationPolicy = name.Trim();
        }
    }

    // ── house rules ─────────────────────────────────────────────────

    /// <summary>
    /// Reads Airbnb's structured "House rules" list (check-in/out, guest cap,
    /// pets/smoking/parties, quiet hours, and the host's free-text additional
    /// rules). Current shape: <c>StaysPdpRuleDetails.groupItems[].items[]</c> of
    /// typed <c>StaysPdpDetailItem</c>s; legacy shape: <c>houseRulesSections[].items[]</c>.
    /// </summary>
    private static void CollectAirbnbHouseRules(JsonElement element, AirbnbStateAccumulator acc)
    {
        if (element.TryGetProperty("groupItems", out var groups) && groups.ValueKind == JsonValueKind.Array)
        {
            foreach (var group in groups.EnumerateArray())
            {
                if (group.ValueKind != JsonValueKind.Object ||
                    !group.TryGetProperty("items", out var items) ||
                    items.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var item in items.EnumerateArray())
                {
                    // Only typed house-rule items; other detail groups (safety,
                    // accessibility) share the same container shape.
                    var type = ReadString(item, "type");
                    if (type is not null && type.StartsWith("HOUSE_RULES_", StringComparison.Ordinal))
                    {
                        InterpretHouseRuleItem(item, type, acc);
                    }
                }
            }
        }

        if (element.TryGetProperty("houseRulesSections", out var sections) && sections.ValueKind == JsonValueKind.Array)
        {
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
                    InterpretHouseRuleItem(item, type: null, acc);
                }
            }
        }
    }

    private static void InterpretHouseRuleItem(JsonElement item, string? type, AirbnbStateAccumulator acc)
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

        // Secondary text: legacy "subtitle" string, or the current "description"
        // which is a LocalizedText { text } for quiet hours and a PdpUGCText for
        // the host's free-text rules.
        var detail = NormalizeSpaces(ReadString(item, "subtitle") ?? ReadLocalizedOrUgcText(item, "description"));

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

        // Check-in windows read "Check-in after 4:00 PM" or "Check-in: 2:00 PM - 8:00 PM";
        // "Self check-in with keypad" has a distinct type and no time to parse.
        var isCheckIn = type is "HOUSE_RULES_CHECK_IN_WINDOW" ||
            (type is null && (Has(title, "check-in") || Has(title, "check in")));
        var isCheckOut = type is "HOUSE_RULES_CHECK_OUT_TIME" ||
            (type is null && (Has(title, "checkout") || Has(title, "check-out") || Has(title, "check out")));

        if (isCheckIn)
        {
            acc.CheckInTime ??= ParseClockTime(title);
        }
        else if (isCheckOut)
        {
            acc.CheckOutTime ??= ParseClockTime(title);
        }

        if (type is "HOUSE_RULES_QUIET_HOURS" || Has(title, "quiet hours"))
        {
            var times = ClockTimeRegex().Matches(detail ?? string.Empty);
            if (times.Count >= 2)
            {
                acc.QuietHoursStart ??= NormalizeClock(times[0].Value);
                acc.QuietHoursEnd ??= NormalizeClock(times[1].Value);
            }
        }

        var isFreeText = type is "HOUSE_RULES_ADDITIONAL_RULES" or "HOUSE_RULES_ADDITIONAL_REQUESTS" ||
            (type is null && (Has(title, "additional rules") || Has(title, "additional requests")));
        if (isFreeText)
        {
            // Legacy items carry the text in html.htmlText; current ones in description.
            string? text = null;
            if (item.TryGetProperty("html", out var html) && html.ValueKind == JsonValueKind.Object)
            {
                text = ReadString(html, "htmlText");
            }

            text ??= ReadLocalizedOrUgcText(item, "description");
            if (!string.IsNullOrWhiteSpace(text) && !acc.HouseRuleParagraphs.Contains(text, StringComparer.Ordinal))
            {
                acc.HouseRuleParagraphs.Add(text);
            }
        }
    }

    // ── photos ──────────────────────────────────────────────────────

    /// <summary>
    /// Cap how many Airbnb gallery photos we surface. Listings often expose
    /// 40–80 CDN URLs; importing all of them makes create/edit timeouts and
    /// saturates storage. Hosts can still upload more from the photo editor.
    /// </summary>
    private const int MaxImportedPhotos = 20;

    /// <summary>
    /// Gallery photos are only taken from containers that are known to hold the
    /// host's listing photos — never from the generic URL scan — because review
    /// icons, host avatars and platform assets share the same CDN prefix.
    /// </summary>
    private static void CollectAirbnbGalleryPhotos(JsonElement element, AirbnbStateAccumulator acc)
    {
        var typename = ReadString(element, "__typename");
        switch (typename)
        {
            // pdpPresentation.heroMedia.edges[].node and mediaTour.stops[].items[]
            case "HeroImageItem":
            case "ImageMediaTourItem":
                if (element.TryGetProperty("image", out var image) && image.ValueKind == JsonValueKind.Object)
                {
                    TryAddAirbnbPhoto(ReadString(image, "uri"), listingId: null, requireListingMarker: false, acc);
                }

                break;

            // sections[].section for sectionId PHOTO_TOUR_SCROLLABLE_MODAL (and its
            // legacy name); mediaItems[] are Image { baseUrl }.
            case "PhotoTourModalSection":
            case "PdpPhotoTourModalSection":
                if (element.TryGetProperty("mediaItems", out var mediaItems) && mediaItems.ValueKind == JsonValueKind.Array)
                {
                    foreach (var media in mediaItems.EnumerateArray())
                    {
                        if (media.ValueKind == JsonValueKind.Object)
                        {
                            TryAddAirbnbPhoto(ReadString(media, "baseUrl"), listingId: null, requireListingMarker: false, acc);
                        }
                    }
                }

                break;

            default:
                break;
        }
    }

    private static void TryAddAirbnbPhoto(
        string? value,
        string? listingId,
        bool requireListingMarker,
        AirbnbStateAccumulator acc)
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

        // Never platform assets (favicons, review icons, map pins) or user avatars.
        if (value.Contains("platform-assets", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("PlatformAssets", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("/im/pictures/user/", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("/user/User/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Strings found by the generic walk are only accepted when they carry the
        // listing-scoped "Hosting-<id>" namespace used by the older photo pipeline.
        if (requireListingMarker)
        {
            var marker = listingId is null ? "Hosting-" : "Hosting-" + listingId;
            if (!value.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
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

    // ── text helpers ────────────────────────────────────────────────

    /// <summary>
    /// Reads Airbnb's <c>UGCText</c> ({ localizedStringWithTranslationPreference |
    /// localizedString | source }) or a <c>PdpUGCText</c> wrapper ({ content: UGCText }).
    /// </summary>
    private static string? ReadUgcText(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (element.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Object)
        {
            return ReadUgcText(content);
        }

        return ReadString(element, "localizedStringWithTranslationPreference") ??
            ReadString(element, "localizedString") ??
            ReadString(element, "source");
    }

    /// <summary>
    /// Reads a property that may be a plain string, a <c>LocalizedText</c> ({ text }),
    /// or a <c>PdpUGCText</c> / <c>UGCText</c>.
    /// </summary>
    private static string? ReadLocalizedOrUgcText(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Object => ReadString(value, "text") ?? ReadUgcText(value),
            _ => null,
        };
    }

    /// <summary>"PRIVATE_SUITE" → "Private suite"; "APARTMENT" → "Apartment".</summary>
    private static string? HumanizeEnumToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var chars = token.Trim().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = chars[i] == '_' ? ' ' : i == 0 ? char.ToUpperInvariant(chars[i]) : char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
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
