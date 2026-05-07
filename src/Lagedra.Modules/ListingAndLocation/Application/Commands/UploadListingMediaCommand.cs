using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

/// <summary>
/// Uploads a single image or video file for a listing through the API to the
/// listings object-storage bucket. Images are added to the photo gallery;
/// videos populate <c>VirtualTourUrl</c>. The bucket is exposed with a
/// public-read policy so URLs can be returned to clients directly.
/// </summary>
public sealed record UploadListingMediaCommand(
    Guid ListingId,
    Guid CallerUserId,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string? Caption,
    Func<CancellationToken, Task<Stream>> OpenReadStream)
    : IRequest<Result<UploadListingMediaResult>>;

public sealed record UploadListingMediaResult(
    string Kind,
    Uri Url,
    string StorageKey,
    ListingPhotoDto? Photo);

public sealed class UploadListingMediaCommandHandler(
    ListingsDbContext dbContext,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions)
    : IRequestHandler<UploadListingMediaCommand, Result<UploadListingMediaResult>>
{
    private const long MaxImageBytes = 15L * 1024 * 1024;
    private const long MaxVideoBytes = 100L * 1024 * 1024;
    private readonly string _bucket = storageOptions.Value.ListingsBucket;

    private static readonly HashSet<string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/heic",
        "image/heif",
    };

    private static readonly HashSet<string> VideoMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4",
        "video/quicktime",
        "video/webm",
    };

    public async Task<Result<UploadListingMediaResult>> Handle(
        UploadListingMediaCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            return Result<UploadListingMediaResult>.Failure(
                new Error("Listing.Media.InvalidFileName", "A file name is required."));
        }

        if (request.SizeBytes <= 0)
        {
            return Result<UploadListingMediaResult>.Failure(
                new Error("Listing.Media.EmptyFile", "The selected file is empty."));
        }

        var mime = string.IsNullOrWhiteSpace(request.MimeType)
            ? "application/octet-stream"
            : request.MimeType;

        var isImage = ImageMimeTypes.Contains(mime);
        var isVideo = VideoMimeTypes.Contains(mime);

        if (!isImage && !isVideo)
        {
            return Result<UploadListingMediaResult>.Failure(
                new Error("Listing.Media.UnsupportedFileType",
                    $"File type '{mime}' is not allowed. Allowed: JPEG, PNG, GIF, WebP, HEIC images or MP4, MOV, WebM videos."));
        }

        if (isImage && request.SizeBytes > MaxImageBytes)
        {
            return Result<UploadListingMediaResult>.Failure(
                new Error("Listing.Media.FileTooLarge",
                    $"Images must be under {MaxImageBytes / (1024 * 1024)} MB."));
        }

        if (isVideo && request.SizeBytes > MaxVideoBytes)
        {
            return Result<UploadListingMediaResult>.Failure(
                new Error("Listing.Media.FileTooLarge",
                    $"Videos must be under {MaxVideoBytes / (1024 * 1024)} MB."));
        }

        var listing = await dbContext.Listings
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<UploadListingMediaResult>.Failure(
                new Error("Listing.NotFound", "Listing not found."));
        }

        if (listing.LandlordUserId != request.CallerUserId)
        {
            return Result<UploadListingMediaResult>.Failure(
                new Error("Listing.Forbidden", "You do not own this listing."));
        }

        var safeFileName = SanitizeFileName(request.OriginalFileName);
        var prefix = isImage ? "photos" : "videos";
        var storageKey = $"listings/{request.ListingId}/{prefix}/{Guid.NewGuid()}/{safeFileName}";

        await storageService.EnsureBucketExistsAsync(_bucket, cancellationToken).ConfigureAwait(false);
        await storageService.EnsurePublicReadPolicyAsync(_bucket, cancellationToken).ConfigureAwait(false);

        var source = await request.OpenReadStream(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            await storageService
                .UploadObjectAsync(_bucket, storageKey, source, mime, cancellationToken)
                .ConfigureAwait(false);
        }

        var url = storageService.GetPublicObjectUrl(_bucket, storageKey);

        if (isVideo)
        {
            listing.SetVirtualTourUrl(url);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result<UploadListingMediaResult>.Success(
                new UploadListingMediaResult("Video", url, storageKey, Photo: null));
        }

        var photo = listing.AddPhoto(storageKey, url, request.Caption);

        // The factory assigns Id before EF tracks the entity; force Added so
        // change detection inserts rather than updates.
        dbContext.Entry(photo).State = EntityState.Added;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var photoDto = new ListingPhotoDto(photo.Id, photo.Url, photo.Caption, photo.IsCover, photo.SortOrder);
        return Result<UploadListingMediaResult>.Success(
            new UploadListingMediaResult("Photo", url, storageKey, photoDto));
    }

    private static string SanitizeFileName(string fileName)
    {
        var cleaned = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(cleaned.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
    }
}
