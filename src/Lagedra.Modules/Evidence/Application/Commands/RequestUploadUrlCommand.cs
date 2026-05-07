using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.Evidence.Application.DTOs;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.Evidence.Application.Commands;

public sealed record RequestUploadUrlCommand(
    Guid ManifestId,
    string FileName,
    string MimeType) : IRequest<Result<UploadUrlDto>>;

public sealed class RequestUploadUrlCommandHandler(
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions)
    : IRequestHandler<RequestUploadUrlCommand, Result<UploadUrlDto>>
{
    private static readonly TimeSpan UploadUrlExpiry = TimeSpan.FromMinutes(30);
    private readonly string _bucket = storageOptions.Value.EvidenceBucket;

    public async Task<Result<UploadUrlDto>> Handle(
        RequestUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var storageKey = $"evidence/{request.ManifestId}/{Guid.NewGuid()}/{request.FileName}";
        var uploadId = Guid.NewGuid();

        await storageService.EnsureBucketExistsAsync(_bucket, cancellationToken)
            .ConfigureAwait(false);

        var presignedUrl = await storageService
            .GeneratePresignedUploadUrlAsync(_bucket, storageKey, UploadUrlExpiry, cancellationToken)
            .ConfigureAwait(false);

        var dto = new UploadUrlDto(uploadId, presignedUrl, storageKey);
        return Result<UploadUrlDto>.Success(dto);
    }
}
