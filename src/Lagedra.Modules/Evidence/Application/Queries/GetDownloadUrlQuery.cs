using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.Evidence.Application.Services;
using Lagedra.Modules.Evidence.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.Evidence.Application.Queries;

public sealed record DownloadUrlDto(Guid UploadId, Uri PresignedUrl, string OriginalFileName);

public sealed record GetDownloadUrlQuery(Guid UploadId, EvidenceCallerContext Caller)
    : IRequest<Result<DownloadUrlDto>>;

public sealed class GetDownloadUrlQueryHandler(
    EvidenceDbContext dbContext,
    EvidenceViewAccessService accessService,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions)
    : IRequestHandler<GetDownloadUrlQuery, Result<DownloadUrlDto>>
{
    private static readonly TimeSpan DownloadUrlExpiry = TimeSpan.FromMinutes(15);
    private readonly string _bucket = storageOptions.Value.EvidenceBucket;

    public async Task<Result<DownloadUrlDto>> Handle(
        GetDownloadUrlQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var access = await accessService
            .RequireUploadViewAsync(request.Caller, request.UploadId, cancellationToken)
            .ConfigureAwait(false);

        if (!access.IsSuccess)
        {
            return Result<DownloadUrlDto>.Failure(access.Error);
        }

        var upload = await dbContext.Uploads
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UploadId, cancellationToken)
            .ConfigureAwait(false);

        if (upload is null)
        {
            return Result<DownloadUrlDto>.Failure(
                new Error("Evidence.UploadNotFound", "Upload not found."));
        }

        var presignedUrl = await storageService
            .GeneratePresignedDownloadUrlAsync(_bucket, upload.StorageKey, DownloadUrlExpiry, cancellationToken)
            .ConfigureAwait(false);

        return Result<DownloadUrlDto>.Success(
            new DownloadUrlDto(upload.Id, presignedUrl, upload.OriginalFileName));
    }
}
