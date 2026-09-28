using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>
/// OwnerRez channel Connection IDs (<c>ora7ccdcead6x</c> and the same shape).
/// The HA-XML connect form accepts only these, so an email or API token cannot
/// be stored as if it were a channel account.
/// </summary>
public static partial class OwnerRezAdvertiserIds
{
    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value) && AdvertiserId().IsMatch(value.Trim());

    /// <summary>
    /// OwnerRez matches these ids in feed URLs with their original casing, which is lowercase.
    /// </summary>
    [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
        Justification = "OwnerRez connection ids are lowercase and feed URLs match that casing.")]
    public static string Canonical(string value) => value.Trim().ToLowerInvariant();

    [GeneratedRegex("^ora[a-z0-9]+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AdvertiserId();
}
