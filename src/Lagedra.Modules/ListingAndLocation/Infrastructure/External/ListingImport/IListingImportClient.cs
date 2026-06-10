namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;

/// <summary>Raw result of fetching a public listing page.</summary>
public sealed record ListingFetchResult(
    string Html,
    Uri FinalUrl,
    string? ContentType);

/// <summary>
/// Server-side fetcher for public listing pages. The browser never calls remote
/// sites directly: all fetching happens here so we control the User-Agent,
/// timeout, redirect depth, and response-size limits, and so the remote site
/// only ever sees Lagedra's IP.
/// </summary>
public interface IListingImportClient
{
    /// <summary>
    /// Fetches the page at <paramref name="url"/>. Returns null when the response
    /// is not retrievable HTML (network error, non-success status, non-HTML
    /// content type, or oversized body).
    /// </summary>
    Task<ListingFetchResult?> FetchAsync(Uri url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches robots.txt for the host of <paramref name="url"/>. Returns null
    /// when robots.txt is absent or unreadable (which the policy treats as
    /// "allowed").
    /// </summary>
    Task<string?> FetchRobotsAsync(Uri url, CancellationToken cancellationToken = default);
}
