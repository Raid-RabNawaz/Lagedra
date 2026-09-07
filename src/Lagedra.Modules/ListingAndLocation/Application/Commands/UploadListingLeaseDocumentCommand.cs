using System.Security.Cryptography;
using Lagedra.Infrastructure.External.Antivirus;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Domain.ValueObjects;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

/// <summary>
/// Uploads a host-authored lease agreement for a listing into the private
/// lease-documents bucket and switches the listing onto it. Unlike listing
/// photos, the file is scanned before it is stored: it is served to
/// prospective tenants, so an infected upload must never reach the bucket.
/// </summary>
public sealed record UploadListingLeaseDocumentCommand(
    Guid ListingId,
    Guid CallerUserId,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    Func<CancellationToken, Task<Stream>> OpenReadStream)
    : IRequest<Result<CustomLeaseDocumentDto>>;

public sealed class UploadListingLeaseDocumentCommandHandler(
    ListingsDbContext dbContext,
    IObjectStorageService storageService,
    IAntivirusService antivirus,
    IOptions<MinioSettings> storageOptions,
    IClock clock)
    : IRequestHandler<UploadListingLeaseDocumentCommand, Result<CustomLeaseDocumentDto>>
{
    private const long MaxDocumentBytes = 10L * 1024 * 1024;
    private const string PdfMimeType = "application/pdf";
    private const string DocxMimeType =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    private readonly string _bucket = storageOptions.Value.LeaseDocumentsBucket;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        PdfMimeType,
        DocxMimeType,
    };

    public async Task<Result<CustomLeaseDocumentDto>> Handle(
        UploadListingLeaseDocumentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.OriginalFileName))
        {
            return Failure("Listing.LeaseDocument.InvalidFileName", "A file name is required.");
        }

        if (request.SizeBytes <= 0)
        {
            return Failure("Listing.LeaseDocument.EmptyFile", "The selected file is empty.");
        }

        if (request.SizeBytes > MaxDocumentBytes)
        {
            return Failure(
                "Listing.LeaseDocument.FileTooLarge",
                $"Lease agreements must be under {MaxDocumentBytes / (1024 * 1024)} MB.");
        }

        var mime = string.IsNullOrWhiteSpace(request.MimeType)
            ? "application/octet-stream"
            : request.MimeType;

        if (!AllowedMimeTypes.Contains(mime))
        {
            return Failure(
                "Listing.LeaseDocument.UnsupportedFileType",
                $"File type '{mime}' is not allowed. Upload a PDF or Word (.docx) document.");
        }

        var listing = await dbContext.Listings
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Failure("Listing.NotFound", "Listing not found.");
        }

        if (listing.LandlordUserId != request.CallerUserId)
        {
            return Failure("Listing.Forbidden", "You do not own this listing.");
        }

        // Buffer once: the file is small and capped above, and both the scan
        // and the upload need to read it from the start.
        byte[] content;
        var source = await request.OpenReadStream(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            content = buffer.ToArray();
        }

        if (content.Length == 0)
        {
            return Failure("Listing.LeaseDocument.EmptyFile", "The selected file is empty.");
        }

        using (var scanStream = new MemoryStream(content, writable: false))
        {
            var scan = await antivirus.ScanAsync(scanStream, cancellationToken).ConfigureAwait(false);
            if (scan.Status == ScanStatus.Infected)
            {
                return Failure(
                    "Listing.LeaseDocument.Infected",
                    $"The uploaded file failed a malware scan ({scan.ThreatName ?? "unknown threat"}).");
            }
        }

        var safeFileName = SanitizeFileName(request.OriginalFileName);
        var storageKey = $"lease-documents/{request.ListingId}/{Guid.NewGuid()}/{safeFileName}";
        var previousKey = listing.CustomLeaseDocument?.StorageKey;

        await storageService.EnsureBucketExistsAsync(_bucket, cancellationToken).ConfigureAwait(false);

        using (var uploadStream = new MemoryStream(content, writable: false))
        {
            await storageService
                .UploadObjectAsync(_bucket, storageKey, uploadStream, mime, cancellationToken)
                .ConfigureAwait(false);
        }

        var document = CustomLeaseDocument.Create(
            storageKey,
            safeFileName,
            mime,
            content.Length,
            Convert.ToHexString(SHA256.HashData(content)),
            clock.UtcNow);

        listing.AttachCustomLeaseDocument(document);
        listing.SetLeaseAgreementSource(LeaseAgreementSource.HostProvided);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Best-effort: a leftover object costs storage but a failed delete must
        // not fail an upload the host has already been told succeeded.
        if (!string.IsNullOrWhiteSpace(previousKey) && previousKey != storageKey)
        {
            try
            {
                await storageService.DeleteObjectAsync(_bucket, previousKey, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                // Ignored: superseded object is unreachable from the listing.
            }
        }

        return Result<CustomLeaseDocumentDto>.Success(new CustomLeaseDocumentDto(
            document.FileName,
            document.ContentType,
            document.SizeBytes,
            document.UploadedAtUtc));
    }

    private static Result<CustomLeaseDocumentDto> Failure(string code, string description) =>
        Result<CustomLeaseDocumentDto>.Failure(new Error(code, description));

    private static string SanitizeFileName(string fileName)
    {
        var cleaned = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(cleaned.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "lease-agreement" : safe;
    }
}
