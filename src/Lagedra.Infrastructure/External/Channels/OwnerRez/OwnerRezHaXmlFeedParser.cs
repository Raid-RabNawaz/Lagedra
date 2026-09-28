using System.Globalization;
using System.Xml.Linq;

namespace Lagedra.Infrastructure.External.Channels.OwnerRez;

/// <summary>One row from an HA-XML index: an external id and the URL of its detail document.</summary>
public sealed record OwnerRezHaXmlContentLink(string ExternalId, string? ContentLocation);

/// <summary>
/// Reads OwnerRez HA-XML (HomeAway 4.2) listing, rate, and availability documents
/// into the provider-agnostic channel snapshots. Element lookup is by local name
/// so a default XML namespace does not hide the fields.
/// </summary>
public static class OwnerRezHaXmlFeedParser
{
    public static IReadOnlyList<OwnerRezHaXmlContentLink> ReadIndex(
        string xml,
        string idLocalName,
        string linkElementName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);
        var doc = XDocument.Parse(xml);

        return doc
            .Descendants()
            .Where(entry => entry.Elements().Any(child => child.Name.LocalName == idLocalName))
            .Where(entry => !IsExplicitlyInactive(entry))
            .Select(entry => new OwnerRezHaXmlContentLink(
                Text(entry, idLocalName) ?? string.Empty,
                Text(entry, linkElementName)))
            .Where(link => link.ExternalId.Length > 0)
            .ToList();
    }

    public static ChannelListingSnapshot? ParseListing(string listingXml, string? rateXml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(listingXml);
        var doc = XDocument.Parse(listingXml);
        var listing = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "listing") ?? doc.Root;
        if (listing is null || IsExplicitlyInactive(listing))
        {
            return null;
        }

        var unit = listing.Descendants().FirstOrDefault(e => e.Name.LocalName == "unit");
        var externalId = DirectText(listing, "externalId") ?? DirectText(unit, "externalId");
        if (string.IsNullOrWhiteSpace(externalId))
        {
            return null;
        }

        var rate = string.IsNullOrWhiteSpace(rateXml) ? null : XDocument.Parse(rateXml);
        var nightly = AverageNightlyRate(rate);
        var currency = Text(rate?.Root, "currency") ?? Text(listing, "currency") ?? "USD";

        var addressParent = listing.Descendants().FirstOrDefault(e => e.Name.LocalName == "address");
        var geo = listing.Descendants().FirstOrDefault(e => e.Name.LocalName == "geoCode");

        return new ChannelListingSnapshot(
            ExternalListingId: externalId,
            Title: LocalizedText(FirstDescendant(listing, "propertyName"))
                ?? LocalizedText(FirstDescendant(listing, "headline"))
                ?? externalId,
            Description: LocalizedText(FirstDescendant(listing, "description")),
            NightlyRateCents: nightly,
            Currency: currency,
            MinStayNights: FirstInt(listing, "minimumStay") ?? FirstInt(rate?.Root, "minimumStay"),
            Bedrooms: CountRooms(listing, "bedroom"),
            Bathrooms: CountBathrooms(listing),
            SquareFootage: FirstInt(listing, "squareFeet") ?? FirstInt(listing, "area"),
            Latitude: FirstDouble(geo, "latDegrees"),
            Longitude: FirstDouble(geo, "lngDegrees"),
            PropertyType: MapPropertyType(DirectText(unit, "propertyType") ?? DirectText(listing, "propertyType")),
            Address: addressParent is null
                ? null
                : new ChannelAddress(
                    Text(addressParent, "addressLine1"),
                    Text(addressParent, "city"),
                    Text(addressParent, "stateOrProvince"),
                    Text(addressParent, "postalCode"),
                    Text(addressParent, "country")),
            AmenityCodes: FeatureNames(listing),
            Photos: Photos(listing),
            PriceBasis: nightly is null ? ChannelPriceBasis.Unknown : ChannelPriceBasis.Nightly);
    }

    public static ChannelAvailabilityCalendar ParseAvailability(string externalListingId, string xml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);
        var doc = XDocument.Parse(xml);
        var availability = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "availability")?.Value?.Trim();
        var beginText = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "beginDate")?.Value?.Trim();

        if (string.IsNullOrWhiteSpace(availability)
            || !DateOnly.TryParse(beginText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var begin))
        {
            return new ChannelAvailabilityCalendar(externalListingId, []);
        }

        var days = new Dictionary<DateOnly, bool>();
        for (var i = 0; i < availability.Length; i++)
        {
            var flag = availability[i];
            if (flag is not ('Y' or 'y' or 'N' or 'n' or 'Q' or 'q'))
            {
                continue;
            }

            days[begin.AddDays(i)] = flag is 'Y' or 'y';
        }

        return new ChannelAvailabilityCalendar(externalListingId, Collapse(days));
    }

    private static long? AverageNightlyRate(XDocument? rate)
    {
        var nightly = rate?.Descendants().FirstOrDefault(e => e.Name.LocalName == "nightlyRates");
        if (nightly is null)
        {
            return null;
        }

        var amounts = nightly
            .Elements()
            .Select(e => decimal.TryParse(e.Value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
                ? amount
                : (decimal?)null)
            .Where(amount => amount is > 0)
            .Select(amount => amount!.Value)
            .ToList();

        if (amounts.Count == 0)
        {
            return null;
        }

        return (long)Math.Round(amounts.Average() * 100m, MidpointRounding.AwayFromZero);
    }

    private static List<string> FeatureNames(XElement listing) =>
        listing
            .Descendants()
            .Where(e => e.Name.LocalName is "unitFeatureName" or "listingFeatureName" or "safetyFeatureName")
            .Select(e => e.Value.Trim())
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static List<ChannelPhoto> Photos(XElement listing) =>
        listing
            .Descendants()
            .Where(e => e.Name.LocalName == "image")
            .Select(image =>
            {
                var urlText = Text(image, "uri") ?? Text(image, "url");
                if (!Uri.TryCreate(urlText, UriKind.Absolute, out var url))
                {
                    return null;
                }

                var id = DirectText(image, "externalId") ?? url.AbsoluteUri;
                return new ChannelPhoto(
                    id,
                    url,
                    LocalizedText(FirstDescendant(image, "title")) ?? DirectText(image, "caption"));
            })
            .Where(photo => photo is not null)
            .Cast<ChannelPhoto>()
            .ToList();

    private static int? CountRooms(XElement listing, string roomLocalName)
    {
        var count = listing.Descendants().Count(e => e.Name.LocalName == roomLocalName);
        return count == 0 ? null : count;
    }

    private static decimal? CountBathrooms(XElement listing)
    {
        var rooms = listing.Descendants().Where(e => e.Name.LocalName == "bathroom").ToList();
        if (rooms.Count == 0)
        {
            return null;
        }

        decimal total = 0;
        foreach (var room in rooms)
        {
            var kind = Text(room, "roomSubType") ?? string.Empty;
            total += kind.Contains("HALF", StringComparison.OrdinalIgnoreCase) ? 0.5m : 1m;
        }

        return total;
    }

    private static string? MapPropertyType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        const string prefix = "PROPERTY_TYPE_";
        var value = raw.Trim().ToUpperInvariant();
        if (value.StartsWith(prefix, StringComparison.Ordinal))
        {
            value = value[prefix.Length..];
        }

        return value switch
        {
            "APARTMENT" or "CONDOMINIUM" => "apartment",
            "CONDO" => "condo",
            "TOWNHOUSE" or "TOWNHOME" => "townhouse",
            "STUDIO" => "studio",
            "LOFT" => "loft",
            "VILLA" => "villa",
            "COTTAGE" => "cottage",
            "CABIN" => "cabin",
            "HOUSE" => "house",
            _ => "other",
        };
    }

    private static string? LocalizedText(XElement? parent)
    {
        if (parent is null)
        {
            return null;
        }

        var texts = parent.Descendants().Where(e => e.Name.LocalName == "text").ToList();
        var chosen = texts.FirstOrDefault(text =>
                (text.Attribute("locale")?.Value ?? string.Empty)
                .StartsWith("en", StringComparison.OrdinalIgnoreCase))
            ?? texts.FirstOrDefault();

        var value = chosen?
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "textValue")
            ?.Value
            .Trim();

        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool IsExplicitlyInactive(XElement element) =>
        element.Elements().Any(child =>
            child.Name.LocalName == "active"
            && child.Value.Trim().Equals("false", StringComparison.OrdinalIgnoreCase));

    private static XElement? FirstDescendant(XElement parent, string localName) =>
        parent.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);

    private static string? DirectText(XElement? parent, string localName)
    {
        var value = parent?.Elements().FirstOrDefault(e => e.Name.LocalName == localName)?.Value.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? Text(XElement? parent, string localName)
    {
        var value = parent?.Descendants().FirstOrDefault(e => e.Name.LocalName == localName)?.Value.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int? FirstInt(XElement? parent, string localName)
    {
        var text = Text(parent, localName);
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static double? FirstDouble(XElement? parent, string localName)
    {
        var text = Text(parent, localName);
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    private static List<ChannelDateBlock> Collapse(Dictionary<DateOnly, bool> days)
    {
        if (days.Count == 0)
        {
            return [];
        }

        var ordered = days.OrderBy(pair => pair.Key).ToList();
        var blocks = new List<ChannelDateBlock>();
        var blockStart = ordered[0].Key;
        var blockAvailable = ordered[0].Value;
        var previous = ordered[0].Key;

        for (var i = 1; i < ordered.Count; i++)
        {
            var (date, available) = ordered[i];
            if (available == blockAvailable && date == previous.AddDays(1))
            {
                previous = date;
                continue;
            }

            blocks.Add(new ChannelDateBlock(blockStart, previous, blockAvailable));
            blockStart = date;
            blockAvailable = available;
            previous = date;
        }

        blocks.Add(new ChannelDateBlock(blockStart, previous, blockAvailable));
        return blocks;
    }
}
