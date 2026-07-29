using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.IdentityAndVerification.Application.Queries;

/// <summary>
/// Admin review detail for one manual verification: the applicant's declared
/// personal info plus short-lived presigned URLs for the uploaded ID photos
/// and live selfie. URLs expire quickly on purpose — the bucket is private.
/// </summary>
public sealed record GetManualVerificationDetailQuery(Guid ProfileId)
    : IRequest<Result<ManualVerificationDetailDto>>;

public sealed record ManualVerificationDocumentDto(
    KycDocumentType DocumentType,
    string FileName,
    string MimeType,
    DateTime UploadedAt,
    Uri DownloadUrl);

public sealed record ManualVerificationDetailDto(
    Guid ProfileId,
    Guid UserId,
    string? Email,
    string? FirstName,
    string? LastName,
    DateTime? DateOfBirth,
    IReadOnlyList<ManualVerificationDocumentDto> Documents);

public sealed class GetManualVerificationDetailQueryHandler(
    IdentityDbContext dbContext,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions,
    IUserEmailResolver emailResolver)
    : IRequestHandler<GetManualVerificationDetailQuery, Result<ManualVerificationDetailDto>>
{
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromMinutes(15);

    public async Task<Result<ManualVerificationDetailDto>> Handle(
        GetManualVerificationDetailQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var profile = await dbContext.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProfileId, cancellationToken)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return Result<ManualVerificationDetailDto>.Failure(
                new Error("Identity.NotFound", "Profile not found."));
        }

        var documents = await dbContext.KycDocuments
            .AsNoTracking()
            .Where(d => d.UserId == profile.UserId)
            .OrderBy(d => d.DocumentType)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var bucket = storageOptions.Value.KycBucket;
        var documentDtos = new List<ManualVerificationDocumentDto>(documents.Count);

        foreach (var doc in documents)
        {
            var url = await storageService
                .GeneratePresignedDownloadUrlAsync(bucket, doc.StorageKey, DownloadUrlExpiry, cancellationToken)
                .ConfigureAwait(false);

            documentDtos.Add(new ManualVerificationDocumentDto(
                doc.DocumentType, doc.FileName, doc.MimeType, doc.UploadedAt, url));
        }

        var email = await emailResolver.GetEmailAsync(profile.UserId, cancellationToken)
            .ConfigureAwait(false);

        return Result<ManualVerificationDetailDto>.Success(new ManualVerificationDetailDto(
            profile.Id, profile.UserId, email,
            profile.FirstName, profile.LastName, profile.DateOfBirth,
            documentDtos));
    }
}
