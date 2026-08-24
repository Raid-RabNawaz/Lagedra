using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

/// <summary>
/// Fetches listing photos from public http(s) URLs on the server (so CORS
/// never blocks them) and stores them through the same listings-bucket
/// pipeline as a host upload. Individual fetch failures are counted and
/// skipped rather than aborting the batch.
/// </summary>
public sealed record ImportListingPhotosFromUrlsCommand(
    Guid ListingId,
    Guid CallerUserId,
    IReadOnlyList<string> Urls) : IRequest<Result<ImportListingPhotosFromUrlsResult>>;

public sealed record ImportListingPhotosFromUrlsResult(int Uploaded, int Failed);

public sealed partial class ImportListingPhotosFromUrlsCommandHandler(
    ListingsDbContext dbContext,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions,
    IHttpClientFactory httpClientFactory,
    ILogger<ImportListingPhotosFromUrlsCommandHandler> logger)
    : IRequestHandler<ImportListingPhotosFromUrlsCommand, Result<ImportListingPhotosFromUrlsResult>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error Forbidden = new("Listing.Forbidden", "You do not own this listing.");

    public async Task<Result<ImportListingPhotosFromUrlsResult>> Handle(
        ImportListingPhotosFromUrlsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<ImportListingPhotosFromUrlsResult>.Failure(NotFound);
        }

        if (listing.LandlordUserId != request.CallerUserId)
        {
            return Result<ImportListingPhotosFromUrlsResult>.Failure(Forbidden);
        }

        var urls = (request.Urls ?? [])
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(ListingMediaImportPolicy.MaxPhotos)
            .ToList();

        if (urls.Count == 0)
        {
            return Result<ImportListingPhotosFromUrlsResult>.Success(new ImportListingPhotosFromUrlsResult(0, 0));
        }

        var bucket = storageOptions.Value.ListingsBucket;
        await storageService.EnsureBucketExistsAsync(bucket, cancellationToken).ConfigureAwait(false);
        await storageService.EnsurePublicReadPolicyAsync(bucket, cancellationToken).ConfigureAwait(false);

        var httpClient = httpClientFactory.CreateClient(ListingMediaImportPolicy.HttpClientName);
        var gate = new SemaphoreSlim(ListingMediaImportPolicy.FetchConcurrency);
        var uploaded = 0;
        var failed = 0;

        FetchedImage?[] results;
        try
        {
            // Sequential persistence (AddPhoto mutates the aggregate) but parallel
            // downloads. Each fetch returns bytes; we then add the photo.
            var fetches = urls.Select((url, index) =>
                FetchImageAsync(httpClient, url, index, gate, cancellationToken));
            results = await Task.WhenAll(fetches).ConfigureAwait(false);
        }
        finally
        {
            gate.Dispose();
        }

        foreach (var fetched in results)
        {
            if (fetched is null)
            {
                failed += 1;
                continue;
            }

            try
            {
                var storageKey =
                    $"listings/{request.ListingId}/photos/{Guid.NewGuid()}/{fetched.FileName}";
                var stream = new MemoryStream(fetched.Bytes, writable: false);
                await using (stream.ConfigureAwait(false))
                {
                    await storageService
                        .UploadObjectAsync(bucket, storageKey, stream, fetched.MimeType, cancellationToken)
                        .ConfigureAwait(false);
                }

                var publicUrl = storageService.GetPublicObjectUrl(bucket, storageKey);
                var photo = listing.AddPhoto(storageKey, publicUrl, caption: null);
                dbContext.Entry(photo).State = EntityState.Added;
                uploaded += 1;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogPhotoImportFailed(logger, fetched.Source.ToString(), ex.Message);
                failed += 1;
            }
        }

        if (uploaded > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return Result<ImportListingPhotosFromUrlsResult>.Success(
            new ImportListingPhotosFromUrlsResult(uploaded, failed));
    }

    private async Task<FetchedImage?> FetchImageAsync(
        HttpClient httpClient,
        string rawUrl,
        int index,
        SemaphoreSlim gate,
        CancellationToken cancellationToken)
    {
        if (!ListingMediaImportPolicy.TryNormalizePublicHttpUrl(rawUrl, out var uri) || uri is null)
        {
            return null;
        }

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var response = await httpClient
                .GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                LogPhotoImportFailed(logger, uri.ToString(), $"HTTP {(int)response.StatusCode}");
                return null;
            }

            if (response.Content.Headers.ContentLength is { } length
                && length > ListingMediaImportPolicy.MaxImageBytes)
            {
                LogPhotoImportFailed(logger, uri.ToString(), "Image exceeds size limit");
                return null;
            }

            var mime = ListingMediaImportPolicy.ResolveImageMime(
                response.Content.Headers.ContentType?.MediaType,
                uri);
            if (mime is null)
            {
                LogPhotoImportFailed(logger, uri.ToString(), "Not an image");
                return null;
            }

            var source = await response.Content
                .ReadAsStreamAsync(cancellationToken)
                .ConfigureAwait(false);
            await using (source.ConfigureAwait(false))
            {
                var buffer = new MemoryStream();
                await using (buffer.ConfigureAwait(false))
                {
                    var copyBuffer = new byte[81920];
                    long total = 0;
                    int read;
                    while ((read = await source.ReadAsync(copyBuffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        total += read;
                        if (total > ListingMediaImportPolicy.MaxImageBytes)
                        {
                            LogPhotoImportFailed(logger, uri.ToString(), "Image exceeds size limit");
                            return null;
                        }

                        await buffer.WriteAsync(copyBuffer.AsMemory(0, read), cancellationToken)
                            .ConfigureAwait(false);
                    }

                    if (total == 0)
                    {
                        return null;
                    }

                    return new FetchedImage(
                        uri,
                        mime,
                        ListingMediaImportPolicy.FileNameFromUrl(uri, index),
                        buffer.ToArray());
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogPhotoImportFailed(logger, uri.ToString(), ex.Message);
            return null;
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record FetchedImage(Uri Source, string MimeType, string FileName, byte[] Bytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Listing photo import skipped {Url}: {Reason}")]
    private static partial void LogPhotoImportFailed(ILogger logger, string url, string reason);
}
