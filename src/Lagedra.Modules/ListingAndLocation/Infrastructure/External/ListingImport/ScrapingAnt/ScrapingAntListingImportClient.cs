using System.Globalization;
using System.Net;
using System.Text;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport.ScrapingAnt;

/// <summary>
/// <see cref="IListingImportClient"/> that fetches the listing page through the
/// ScrapingAnt API. ScrapingAnt loads the URL in a real headless browser behind
/// rotating (residential) proxies and returns the fully rendered HTML, which the
/// existing extractor and AI enricher then parse exactly as before. This is what
/// lets the importer read JavaScript-rendered / bot-protected listings that a
/// plain server-side HTTP fetch cannot.
///
/// robots.txt is still fetched directly (it is public, cheap, and needs no JS),
/// so the same <see cref="ListingImportPolicy"/> guardrails continue to apply.
/// </summary>
public sealed partial class ScrapingAntListingImportClient : IListingImportClient
{
    private readonly HttpClient _antClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ScrapingAntSettings _settings;
    private readonly ILogger<ScrapingAntListingImportClient> _logger;

    public ScrapingAntListingImportClient(
        HttpClient antClient,
        IHttpClientFactory httpClientFactory,
        IOptions<ScrapingAntSettings> options,
        ILogger<ScrapingAntListingImportClient> logger)
    {
        ArgumentNullException.ThrowIfNull(antClient);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _antClient = antClient;
        _httpClientFactory = httpClientFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<ListingFetchResult?> FetchAsync(Uri url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);

        var requestUri = BuildScrapeUri(url);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            using var response = await _antClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            var body = await ReadLimitedAsync(response, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                // ScrapingAnt reports failures as a JSON { "detail": "..." } body.
                LogScrapeFailed(_logger, (int)response.StatusCode, url.Host, Summarize(body));
                return null;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                LogEmptyBody(_logger, url.Host);
                return null;
            }

            // /v2/general returns the rendered HTML directly. We keep the
            // requested URL as the final URL since ScrapingAnt does not surface
            // the post-redirect location; the extractor also reads og:url/canonical.
            return new ListingFetchResult(body, url, "text/html");
        }
        catch (HttpRequestException ex)
        {
            LogScrapeException(_logger, ex, url.Host);
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogScrapeTimeout(_logger, url.Host);
            return null;
        }
    }

    public async Task<string?> FetchRobotsAsync(Uri url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);

        var robotsUri = new Uri(url, "/robots.txt");
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = ListingImportPolicy.FetchTimeout;
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", ListingImportPolicy.UserAgent);

            using var response = await client
                .GetAsync(robotsUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound || !response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    private Uri BuildScrapeUri(Uri target)
    {
        // ScrapingAnt expects query-string parameters in urlencoded form. The
        // base address already ends in ".../v2/", so we append "general".
        var query = new StringBuilder("general?");
        query.Append("url=").Append(Uri.EscapeDataString(target.ToString()));
        query.Append("&browser=").Append(_settings.Browser ? "true" : "false");
        query.Append("&return_page_source=").Append(_settings.Browser ? "false" : "true");

        if (!string.IsNullOrWhiteSpace(_settings.ProxyType))
        {
            query.Append("&proxy_type=").Append(Uri.EscapeDataString(_settings.ProxyType));
        }

        if (!string.IsNullOrWhiteSpace(_settings.ProxyCountry))
        {
            query.Append("&proxy_country=").Append(Uri.EscapeDataString(_settings.ProxyCountry));
        }

        if (_settings.Browser && !string.IsNullOrWhiteSpace(_settings.WaitForSelector))
        {
            query.Append("&wait_for_selector=").Append(Uri.EscapeDataString(_settings.WaitForSelector));
        }

        if (_settings.Browser && !string.IsNullOrWhiteSpace(_settings.BlockResources))
        {
            foreach (var resource in _settings.BlockResources.Split(
                ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                query.Append("&block_resource=").Append(Uri.EscapeDataString(resource));
            }
        }

        var timeout = Math.Clamp(_settings.TimeoutSeconds, 5, 60);
        query.Append("&timeout=").Append(timeout.ToString(CultureInfo.InvariantCulture));

        return new Uri(query.ToString(), UriKind.Relative);
    }

    private async Task<string?> ReadLimitedAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        await using (stream.ConfigureAwait(false))
        {
            var buffer = new byte[81920];
            using var accumulator = new MemoryStream();
            int read;
            while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (accumulator.Length + read > _settings.MaxResponseBytes)
                {
                    LogOversized(_logger, _settings.MaxResponseBytes);
                    return null;
                }

                await accumulator
                    .WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                    .ConfigureAwait(false);
            }

            return Encoding.UTF8.GetString(accumulator.ToArray());
        }
    }

    private static string Summarize(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "(empty)";
        }

        var trimmed = body.Trim();
        return trimmed.Length > 300 ? trimmed[..300] : trimmed;
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "ScrapingAnt returned {StatusCode} for {Host}. Detail: {Detail}")]
    private static partial void LogScrapeFailed(ILogger logger, int statusCode, string host, string detail);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "ScrapingAnt returned an empty body for {Host}.")]
    private static partial void LogEmptyBody(ILogger logger, string host);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "ScrapingAnt request failed for {Host}.")]
    private static partial void LogScrapeException(ILogger logger, Exception exception, string host);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "ScrapingAnt request timed out for {Host}.")]
    private static partial void LogScrapeTimeout(ILogger logger, string host);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "ScrapingAnt response exceeded the {MaxBytes}-byte cap; refusing to parse.")]
    private static partial void LogOversized(ILogger logger, long maxBytes);
}
