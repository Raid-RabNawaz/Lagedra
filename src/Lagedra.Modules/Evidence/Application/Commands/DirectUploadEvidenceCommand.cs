using System.Security.Cryptography;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.Evidence.Application.DTOs;
using Lagedra.Modules.Evidence.Domain.Entities;
using Lagedra.Modules.Evidence.Domain.ValueObjects;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.Evidence.Application.Commands;

/// <summary>
/// Single-call upload that proxies the file through the API to object storage
/// and atomically writes the manifest row. Used by the web client to avoid
/// browser → bucket CORS configuration and to surface a single clear error.
/// </summary>
public sealed record DirectUploadEvidenceCommand(
    Guid ManifestId,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    Func<CancellationToken, Task<Stream>> OpenReadStream)
    : IRequest<Result<ManifestUploadDto>>;

public sealed class DirectUploadEvidenceCommandHandler(
    EvidenceDbContext dbContext,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions)
    : IRequestHandler<DirectUploadEvidenceCommand, Result<ManifestUploadDto>>
{
    private const long MaxFileSizeBytes = 50L * 1024 * 1024;
    private readonly string _bucket = storageOptions.Value.EvidenceBucket;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp",
        "image/heic",
        "image/heif",
        "video/mp4",
        "video/quicktime",
        "video/webm",
        "audio/mpeg",
        "audio/mp4",
        "audio/wav",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
        "text/csv",
    };

    public async Task<Result<ManifestUploadDto>> Handle(
        DirectUploadEvidenceCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            return Result<ManifestUploadDto>.Failure(
                new Error("Evidence.InvalidFileName", "A file name is required."));
        }

        if (request.SizeBytes <= 0)
        {
            return Result<ManifestUploadDto>.Failure(
                new Error("Evidence.EmptyFile", "The selected file is empty."));
        }

        if (request.SizeBytes > MaxFileSizeBytes)
        {
            return Result<ManifestUploadDto>.Failure(
                new Error("Evidence.FileTooLarge",
                    $"File exceeds the {MaxFileSizeBytes / (1024 * 1024)} MB limit."));
        }

        var mime = string.IsNullOrWhiteSpace(request.MimeType)
            ? "application/octet-stream"
            : request.MimeType;

        if (!AllowedMimeTypes.Contains(mime))
        {
            return Result<ManifestUploadDto>.Failure(
                new Error("Evidence.UnsupportedFileType",
                    $"File type '{mime}' is not allowed. Allowed: images, video, audio, PDF, Office documents, plain text."));
        }

        var manifest = await dbContext.Manifests
            .Include(m => m.Uploads)
            .FirstOrDefaultAsync(m => m.Id == request.ManifestId, cancellationToken)
            .ConfigureAwait(false);

        if (manifest is null)
        {
            return Result<ManifestUploadDto>.Failure(
                new Error("Evidence.ManifestNotFound", "Manifest not found."));
        }

        var safeFileName = SanitizeFileName(request.OriginalFileName);
        var storageKey = $"evidence/{request.ManifestId}/{Guid.NewGuid()}/{safeFileName}";

        await storageService.EnsureBucketExistsAsync(_bucket, cancellationToken).ConfigureAwait(false);

        string fileHashHex;
        var source = await request.OpenReadStream(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            // Buffer to a seekable stream so we can both hash and upload in a
            // single read. Files are capped at 50 MB above so this is bounded.
            var buffered = new MemoryStream(capacity: (int)Math.Min(request.SizeBytes, int.MaxValue));
            await using (buffered.ConfigureAwait(false))
            {
                await source.CopyToAsync(buffered, cancellationToken).ConfigureAwait(false);
                buffered.Position = 0;

                using (var sha = SHA256.Create())
                {
                    var hash = await sha.ComputeHashAsync(buffered, cancellationToken).ConfigureAwait(false);
                    fileHashHex = Convert.ToHexString(hash);
                }

                buffered.Position = 0;
                await storageService
                    .UploadObjectAsync(_bucket, storageKey, buffered, mime, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var upload = manifest.AddUpload(safeFileName, storageKey, mime);
        dbContext.Entry(upload).State = EntityState.Added;
        upload.SetFileHash(FileHash.Create(fileHashHex));

        var scanResult = MalwareScanResult.CreatePending(upload.Id);
        dbContext.ScanResults.Add(scanResult);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ManifestUploadDto>.Success(new ManifestUploadDto(
            upload.Id,
            upload.OriginalFileName,
            upload.MimeType,
            upload.FileHash?.Value,
            upload.UploadedAt));
    }

    private static string SanitizeFileName(string fileName)
    {
        var cleaned = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(cleaned.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "file" : safe;
    }
}
