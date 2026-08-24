namespace Lagedra.Modules.Analytics.Application;

/// <summary>
/// Turns stored listing-origin fields (plus an optional channel map) into
/// the admin-facing label: "Hostaway", "URL (airbnb.com)", "Excel import", etc.
/// </summary>
public static class ListingAddedViaFormatter
{
    private static readonly Dictionary<string, string> ProviderLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hostaway"] = "Hostaway",
        ["ownerrez"] = "OwnerRez",
        ["guesty"] = "Guesty",
        ["smoobu"] = "Smoobu",
        ["lodgify"] = "Lodgify",
        ["hosthub"] = "Hosthub",
    };

    public static string Format(string? addedVia, string? detail, string? channelProviderKey)
    {
        if (!string.IsNullOrWhiteSpace(channelProviderKey))
        {
            return ProviderLabel(channelProviderKey);
        }

        return addedVia switch
        {
            "Url" => string.IsNullOrWhiteSpace(detail) ? "URL" : $"URL ({detail.Trim()})",
            "Excel" => "Excel import",
            "Xml" => "XML import",
            "Channel" => ProviderLabel(detail),
            _ => "Manual",
        };
    }

    private static string ProviderLabel(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return "Channel";
        }

        var trimmed = key.Trim();
        if (ProviderLabels.TryGetValue(trimmed, out var label))
        {
            return label;
        }

        return char.ToUpperInvariant(trimmed[0]) + trimmed[1..];
    }
}
