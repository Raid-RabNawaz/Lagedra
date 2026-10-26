namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;

/// <summary>
/// Cheap, string-level checks that decide whether a fetched page is worth
/// handing to the extractor or whether the fetcher should try harder. Used by
/// <see cref="FallbackListingImportClient"/> to decide when to escalate to the
/// paid fetcher; deliberately conservative so a legitimately plain page is not
/// mistaken for a block.
/// </summary>
public static class ListingPageQuality
{
    public enum Verdict
    {
        Usable,
        Missing,
        Empty,
        BotChallenge,
        AirbnbShell,
    }

    /// <summary>
    /// Phrases that only appear on anti-bot interstitials (Cloudflare, Akamai,
    /// PerimeterX, DataDome, Imperva). Matched case-insensitively against the
    /// whole body, so each must be specific enough not to occur in listing copy.
    /// </summary>
    private static readonly string[] BotChallengeMarkers =
    [
        "Pardon Our Interruption",
        "Please verify you are a human",
        "Just a moment...",
        "Attention Required! | Cloudflare",
        "cf-challenge-running",
        "challenge-platform/h/",
        "px-captcha",
        "captcha-delivery.com",
        "_Incapsula_Resource",
        "Access Denied</h1>",
        "Request unsuccessful. Incapsula",
    ];

    /// <summary>Minimum body length below which a page is treated as empty.</summary>
    private const int MinimumUsefulLength = 512;

    public static Verdict Assess(ListingFetchResult? result, Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);

        if (result is null)
        {
            return Verdict.Missing;
        }

        var html = result.Html;
        if (string.IsNullOrWhiteSpace(html) || html.Length < MinimumUsefulLength)
        {
            return Verdict.Empty;
        }

        foreach (var marker in BotChallengeMarkers)
        {
            if (html.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return Verdict.BotChallenge;
            }
        }

        // Airbnb serves listing data in a server-rendered JSON blob plus JSON-LD.
        // A response with neither is the client-side shell (or a disguised block)
        // and would import as an empty draft.
        if (IsAirbnbHost(url) &&
            !html.Contains("data-deferred-state", StringComparison.Ordinal) &&
            !html.Contains("application/ld+json", StringComparison.Ordinal))
        {
            return Verdict.AirbnbShell;
        }

        return Verdict.Usable;
    }

    private static bool IsAirbnbHost(Uri url) =>
        url.Host.Contains("airbnb.", StringComparison.OrdinalIgnoreCase);
}
