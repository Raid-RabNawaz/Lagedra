using System.Net;
using System.Text;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.External.ListingImport;

/// <summary>
/// <see cref="HttpClient"/>-backed implementation of <see cref="IListingImportClient"/>.
/// Enforces the timeout, User-Agent, redirect depth, response-size cap and
/// content-type rules defined by <see cref="ListingImportPolicy"/>.
/// </summary>
public sealed partial class HttpListingImportClient(
    HttpClient httpClient,
    ILogger<HttpListingImportClient> logger)
    : IListingImportClient
{
    private static readonly string[] AllowedContentTypes =
    [
        "text/html",
        "application/xhtml+xml",
    ];

    public async Task<ListingFetchResult?> FetchAsync(Uri url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml");

            using var response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogUnsuccessful(logger, (int)response.StatusCode, url.Host);
                return null;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType is null ||
                !AllowedContentTypes.Contains(mediaType, StringComparer.OrdinalIgnoreCase))
            {
                LogRejectedContentType(logger, mediaType ?? "(none)", url.Host);
                return null;
            }

            var declaredLength = response.Content.Headers.ContentLength;
            if (declaredLength.HasValue && declaredLength.Value > ListingImportPolicy.MaxResponseBytes)
            {
                LogRejectedOversized(logger, declaredLength.Value, url.Host);
                return null;
            }

            var html = await ReadLimitedAsync(response, cancellationToken).ConfigureAwait(false);
            if (html is null)
            {
                return null;
            }

            var finalUrl = response.RequestMessage?.RequestUri ?? url;
            return new ListingFetchResult(html, finalUrl, mediaType);
        }
        catch (HttpRequestException ex)
        {
            LogFetchFailed(logger, ex, url.Host);
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            LogFetchTimeout(logger, url.Host);
            return null;
        }
    }

    public async Task<string?> FetchRobotsAsync(Uri url, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(url);

        var robotsUri = new Uri(url, "/robots.txt");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, robotsUri);
            using var response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotFound ||
                !response.IsSuccessStatusCode)
            {
                return null;
            }

            return await ReadLimitedAsync(response, cancellationToken).ConfigureAwait(false);
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

    private static async Task<string?> ReadLimitedAsync(
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
                if (accumulator.Length + read > ListingImportPolicy.MaxResponseBytes)
                {
                    // Body exceeds the cap; refuse rather than partially parse.
                    return null;
                }

                await accumulator
                    .WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                    .ConfigureAwait(false);
            }

            return Encoding.UTF8.GetString(accumulator.ToArray());
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Listing import fetch returned {StatusCode} for {Host}.")]
    private static partial void LogUnsuccessful(ILogger logger, int statusCode, string host);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Listing import rejected non-HTML content type '{ContentType}' for {Host}.")]
    private static partial void LogRejectedContentType(ILogger logger, string contentType, string host);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Listing import rejected oversized body ({Bytes} bytes) for {Host}.")]
    private static partial void LogRejectedOversized(ILogger logger, long bytes, string host);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Listing import fetch failed for {Host}.")]
    private static partial void LogFetchFailed(ILogger logger, Exception exception, string host);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Listing import fetch timed out for {Host}.")]
    private static partial void LogFetchTimeout(ILogger logger, string host);
}
