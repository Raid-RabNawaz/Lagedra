using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.Evidence.Application.DTOs;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.Evidence.Application.Commands;

public sealed record RequestUploadUrlCommand(
    Guid ManifestId,
    string FileName,
    string MimeType) : IRequest<Result<UploadUrlDto>>;

public sealed class RequestUploadUrlCommandHandler(
    IObjectStorageService storageService)
    : IRequestHandler<RequestUploadUrlCommand, Result<UploadUrlDto>>
{
    private const string EvidenceBucket = "lagedra-evidence";
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(30);

    public async Task<Result<UploadUrlDto>> Handle(
        RequestUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var storageKey = $"evidence/{request.ManifestId}/{Guid.NewGuid()}/{request.FileName}";
        var uploadId = Guid.NewGuid();

        await storageService.EnsureBucketExistsAsync(EvidenceBucket, cancellationToken)
            .ConfigureAwait(false);

        var presignedUrl = await storageService
            .GeneratePresignedUploadUrlAsync(EvidenceBucket, storageKey, UploadUrlExpiry, cancellationToken)
            .ConfigureAwait(false);

        var dto = new UploadUrlDto(uploadId, presignedUrl, storageKey);
        return Result<UploadUrlDto>.Success(dto);
    }
}
