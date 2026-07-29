using System.Diagnostics.CodeAnalysis;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

/// <summary>
/// Uploads one manual-KYC document (ID front/back or live selfie) into the
/// private KYC bucket. Re-uploading the same document type replaces the
/// previous file while the submission is still editable.
/// </summary>
public sealed record UploadKycDocumentCommand(
    Guid UserId,
    KycDocumentType DocumentType,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    Func<CancellationToken, Task<Stream>> OpenReadStream)
    : IRequest<Result<KycDocumentDto>>;

public sealed record KycDocumentDto(
    KycDocumentType DocumentType,
    string FileName,
    DateTime UploadedAt);

public sealed partial class UploadKycDocumentCommandHandler(
    IdentityDbContext dbContext,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions,
    ILogger<UploadKycDocumentCommandHandler> logger)
    : IRequestHandler<UploadKycDocumentCommand, Result<KycDocumentDto>>
{
    private const long MaxImageBytes = 10L * 1024 * 1024;
    private readonly string _bucket = storageOptions.Value.KycBucket;
    private readonly ILogger<UploadKycDocumentCommandHandler> _logger = logger;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/heic",
        "image/heif",
    };

    public async Task<Result<KycDocumentDto>> Handle(
        UploadKycDocumentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.OriginalFileName) || request.SizeBytes <= 0)
        {
            return Result<KycDocumentDto>.Failure(
                new Error("Identity.Kyc.EmptyFile", "A non-empty file is required."));
        }

        if (!AllowedMimeTypes.Contains(request.MimeType))
        {
            return Result<KycDocumentDto>.Failure(
                new Error("Identity.Kyc.UnsupportedFileType",
                    $"File type '{request.MimeType}' is not allowed. Allowed: JPEG, PNG, WebP, HEIC."));
        }

        if (request.SizeBytes > MaxImageBytes)
        {
            return Result<KycDocumentDto>.Failure(
                new Error("Identity.Kyc.FileTooLarge",
                    $"KYC documents must be under {MaxImageBytes / (1024 * 1024)} MB."));
        }

        var profile = await dbContext.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (profile?.Status == VerificationStatus.Verified)
        {
            return Result<KycDocumentDto>.Failure(
                new Error("Identity.Kyc.AlreadyVerified", "Your identity is already verified."));
        }

        if (profile?.Status == VerificationStatus.ManualReviewRequired)
        {
            return Result<KycDocumentDto>.Failure(
                new Error("Identity.Kyc.UnderReview",
                    "Your submission is under review and can no longer be changed."));
        }

        var safeFileName = SanitizeFileName(request.OriginalFileName);
        var storageKey = $"kyc/{request.UserId}/{request.DocumentType}/{Guid.NewGuid()}/{safeFileName}";

        // Private bucket on purpose — KYC documents must never be publicly
        // readable. Admin review uses short-lived presigned URLs.
        await storageService.EnsureBucketExistsAsync(_bucket, cancellationToken).ConfigureAwait(false);

        var source = await request.OpenReadStream(cancellationToken).ConfigureAwait(false);
        await using (source.ConfigureAwait(false))
        {
            await storageService
                .UploadObjectAsync(_bucket, storageKey, source, request.MimeType, cancellationToken)
                .ConfigureAwait(false);
        }

        var previous = await dbContext.KycDocuments
            .Where(d => d.UserId == request.UserId && d.DocumentType == request.DocumentType)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        dbContext.KycDocuments.RemoveRange(previous);

        var document = KycDocument.Create(
            request.UserId, request.DocumentType, storageKey,
            safeFileName, request.MimeType, request.SizeBytes);

        dbContext.KycDocuments.Add(document);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var old in previous)
        {
            await TryDeleteObjectAsync(old.StorageKey, cancellationToken).ConfigureAwait(false);
        }

        return Result<KycDocumentDto>.Success(
            new KycDocumentDto(document.DocumentType, document.FileName, document.UploadedAt));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Best-effort cleanup of replaced objects; storage failures must not mask the successful upload.")]
    private async Task TryDeleteObjectAsync(string storageKey, CancellationToken ct)
    {
        try
        {
            await storageService.DeleteObjectAsync(_bucket, storageKey, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogCleanupFailed(_logger, ex, _bucket, storageKey);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var cleaned = Path.GetFileName(fileName);
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(cleaned.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "document" : safe;
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to delete replaced KYC object {Bucket}/{Key}.")]
    private static partial void LogCleanupFailed(ILogger logger, Exception exception, string bucket, string key);
}
