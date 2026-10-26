using System.Diagnostics.CodeAnalysis;
using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

/// <summary>
/// Streams one uploaded ID photo or live selfie for the admin review dialog.
/// The browser loads these bytes through the authenticated API so the private
/// bucket does not have to be reachable from the reviewer's machine.
/// </summary>
public sealed record GetManualVerificationDocumentQuery(Guid ProfileId, KycDocumentType DocumentType)
    : IRequest<Result<ManualVerificationDocumentFile>>;

[SuppressMessage(
    "Performance", "CA1819:Properties should not return arrays",
    Justification = "Document bytes are a fixed payload streamed to the HTTP response.")]
public sealed record ManualVerificationDocumentFile(string ContentType, byte[] Content);

public sealed partial class GetManualVerificationDocumentQueryHandler(
    IdentityDbContext dbContext,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions,
    ILogger<GetManualVerificationDocumentQueryHandler> logger)
    : IRequestHandler<GetManualVerificationDocumentQuery, Result<ManualVerificationDocumentFile>>
{
    private readonly string _bucket = storageOptions.Value.KycBucket;
    private readonly ILogger<GetManualVerificationDocumentQueryHandler> _logger = logger;

    public async Task<Result<ManualVerificationDocumentFile>> Handle(
        GetManualVerificationDocumentQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return Result<ManualVerificationDocumentFile>.Failure(
                new Error("Identity.NotFound", "Profile not found."));
        }

        var document = await dbContext.KycDocuments
            .AsNoTracking()
            .Where(d => d.UserId == profile.UserId && d.DocumentType == request.DocumentType)
            .OrderByDescending(d => d.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (document is null)
        {
            return Result<ManualVerificationDocumentFile>.Failure(
                new Error("Identity.Kyc.DocumentNotFound", "That document was not uploaded."));
        }

        try
        {
            var source = await storageService
                .GetObjectStreamAsync(_bucket, document.StorageKey, cancellationToken)
                .ConfigureAwait(false);

            await using (source.ConfigureAwait(false))
            {
                using var buffer = new MemoryStream();
                await source.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (buffer.Length == 0)
                {
                    return Result<ManualVerificationDocumentFile>.Failure(
                        new Error("Identity.Kyc.ReadFailed", "The document could not be read."));
                }

                return Result<ManualVerificationDocumentFile>.Success(
                    new ManualVerificationDocumentFile(DisplayContentType(document.MimeType), buffer.ToArray()));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogReadFailed(_logger, ex, request.DocumentType, request.ProfileId);
            return Result<ManualVerificationDocumentFile>.Failure(
                new Error("Identity.Kyc.ReadFailed", "The document could not be read."));
        }
    }

    private static string DisplayContentType(string mimeType)
    {
        return mimeType.Trim().ToUpperInvariant() switch
        {
            "IMAGE/JPEG" or "IMAGE/JPG" => "image/jpeg",
            "IMAGE/PNG" => "image/png",
            "IMAGE/WEBP" => "image/webp",
            "IMAGE/HEIC" or "IMAGE/HEIC-SEQUENCE" => "image/heic",
            "IMAGE/HEIF" or "IMAGE/HEIF-SEQUENCE" => "image/heif",
            "APPLICATION/PDF" => "application/pdf",
            _ => "application/octet-stream",
        };
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to read KYC document {DocumentType} for profile {ProfileId}.")]
    private static partial void LogReadFailed(
        ILogger logger,
        Exception exception,
        KycDocumentType documentType,
        Guid profileId);
}
