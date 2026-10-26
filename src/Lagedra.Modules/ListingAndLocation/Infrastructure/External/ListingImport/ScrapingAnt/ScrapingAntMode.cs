namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.ScrapingAnt;

/// <summary>
/// Order in which ScrapingAnt and the direct <c>HttpListingImportClient</c> are
/// tried when a ScrapingAnt API key is configured. Bound from
/// <c>ListingImport:ScrapingAnt:Mode</c>.
/// </summary>
public enum ScrapingAntMode
{
    /// <summary>
    /// Default. Every fetch goes through ScrapingAnt; the direct fetcher is only
    /// used when ScrapingAnt fails (quota exceeded, subscription ended, API
    /// error, timeout) or returns an unusable page.
    /// </summary>
    Primary = 0,

    /// <summary>
    /// Fetch directly first; call ScrapingAnt only when the direct fetch fails
    /// or returns a bot wall / empty shell. Spends credits only when needed.
    /// </summary>
    Fallback = 1,
}
