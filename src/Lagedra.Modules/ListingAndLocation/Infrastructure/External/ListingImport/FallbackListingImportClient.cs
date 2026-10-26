using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;

/// <summary>
/// <see cref="IListingImportClient"/> that tries a primary fetcher first and only
/// falls back to a secondary one when the primary fails or returns a page that
/// does not look like a real listing — an API error / exhausted quota (null), a
/// bot-challenge interstitial, or an Airbnb shell without the server-rendered
/// state blob.
///
/// The order is decided at registration. Default is ScrapingAnt as primary with
/// the direct <see cref="HttpListingImportClient"/> as the safety net, so an
/// expired subscription or exceeded credit limit degrades to a direct fetch
/// instead of a failed import. The reverse order (direct first, ScrapingAnt as
/// insurance) is also supported; see <c>ScrapingAntSettings.Mode</c>.
///
/// robots.txt is always fetched by the secondary client: both concrete clients
/// fetch it directly, and the secondary is the one that works without credits.
/// </summary>
public sealed partial class FallbackListingImportClient : IListingImportClient
{
    private readonly IListingImportClient _primary;
    private readonly IListingImportClient _secondary;
    private readonly string _primaryName;
    private readonly string _secondaryName;
    private readonly ILogger<FallbackListingImportClient> _logger;

    public FallbackListingImportClient(
        IListingImportClient primary,
        IListingImportClient secondary,
        ILogger<FallbackListingImportClient> logger)
    {
        ArgumentNullException.ThrowIfNull(primary);
        ArgumentNullException.ThrowIfNull(secondary);
        ArgumentNullException.ThrowIfNull(logger);

        _primary = primary;
        _secondary = secondary;
        _primaryName = primary.GetType().Name;
        _secondaryName = secondary.GetType().Name;
        _logger = logger;
    }

    /// <summary>Client tried first on every fetch.</summary>
    public IListingImportClient Primary => _primary;

    /// <summary>Client used when <see cref="Primary"/> fails or returns an unusable page.</summary>
    public IListingImportClient Secondary => _secondary;

    public async Task<ListingFetchResult?> FetchAsync(Uri url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);

        var viaPrimary = await _primary.FetchAsync(url, cancellationToken).ConfigureAwait(false);
        var verdict = ListingPageQuality.Assess(viaPrimary, url);
        if (verdict == ListingPageQuality.Verdict.Usable)
        {
            return viaPrimary;
        }

        LogFallingBack(_logger, _primaryName, url.Host, verdict, _secondaryName);

        var viaSecondary = await _secondary.FetchAsync(url, cancellationToken).ConfigureAwait(false);
        if (viaSecondary is not null &&
            ListingPageQuality.Assess(viaSecondary, url) == ListingPageQuality.Verdict.Usable)
        {
            LogFallbackSucceeded(_logger, _secondaryName, url.Host);
            return viaSecondary;
        }

        // Both paths came back thin. Prefer whatever HTML we have: the generic
        // extractor can still read <title>/OG tags from a partial page, and the
        // caller already treats null as "nothing could be fetched".
        LogFallbackFailed(_logger, _secondaryName, url.Host);
        return viaSecondary ?? viaPrimary;
    }

    public Task<string?> FetchRobotsAsync(Uri url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);
        return _secondary.FetchRobotsAsync(url, cancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "{Primary} fetch of {Host} was {Verdict}; retrying through {Secondary}.")]
    private static partial void LogFallingBack(
        ILogger logger, string primary, string host, ListingPageQuality.Verdict verdict, string secondary);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "{Secondary} recovered a usable page for {Host}.")]
    private static partial void LogFallbackSucceeded(ILogger logger, string secondary, string host);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "{Secondary} did not recover a usable page for {Host} either.")]
    private static partial void LogFallbackFailed(ILogger logger, string secondary, string host);
}
