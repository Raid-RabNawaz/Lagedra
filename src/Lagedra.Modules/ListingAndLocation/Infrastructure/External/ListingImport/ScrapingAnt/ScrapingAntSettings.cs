using System.Diagnostics.CodeAnalysis;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.ScrapingAnt;

/// <summary>
/// Optional configuration for fetching listing pages through the ScrapingAnt
/// web-scraping API (https://scrapingant.com). ScrapingAnt renders the page in
/// a real headless browser behind rotating proxies, which is what allows us to
/// read JavaScript-rendered listings (e.g. Airbnb) that block plain server-side
/// HTTP fetches. When <see cref="ApiKey"/> is empty the integration is never
/// registered and the importer falls back to the direct <c>HttpClient</c>
/// fetcher, so default behaviour is unchanged.
/// </summary>
public sealed class ScrapingAntSettings
{
    public const string SectionName = "ListingImport:ScrapingAnt";

    /// <summary>
    /// ScrapingAnt API key. Empty disables the integration entirely.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>Base address of the ScrapingAnt v2 API.</summary>
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings",
        Justification = "Bound from configuration; validated and converted to a Uri at registration.")]
    public string BaseUrl { get; set; } = "https://api.scrapingant.com/v2/";

    /// <summary>
    /// Proxy pool to route through. <c>residential</c> is required for tough
    /// anti-bot walls such as Airbnb; <c>datacenter</c> is cheaper for sites
    /// that do not actively block scraping.
    /// </summary>
    public string ProxyType { get; set; } = "residential";

    /// <summary>Optional two-letter proxy country (e.g. "us"). Empty = any.</summary>
    public string? ProxyCountry { get; set; }

    /// <summary>
    /// Whether ScrapingAnt should execute the page's JavaScript. Must be true to
    /// recover data from client-rendered listing pages.
    /// </summary>
    public bool Browser { get; set; } = true;

    /// <summary>
    /// Optional CSS selector ScrapingAnt waits for before returning, useful to
    /// ensure dynamic content has mounted. Empty = wait for default page load.
    /// </summary>
    public string? WaitForSelector { get; set; }

    /// <summary>
    /// Resource types ScrapingAnt should skip downloading (comma-separated).
    /// Blocking images/media/fonts speeds up rendering and lowers credit/bandwidth
    /// cost without affecting the metadata we read (og:image URLs etc. remain in
    /// the DOM regardless of whether the bytes were downloaded).
    /// </summary>
    public string BlockResources { get; set; } = "image,media,font";

    /// <summary>
    /// ScrapingAnt-side render timeout in seconds (its API accepts 5–60).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>Largest rendered body we will read, in bytes (15 MB).</summary>
    public long MaxResponseBytes { get; set; } = 15L * 1024 * 1024;
}
