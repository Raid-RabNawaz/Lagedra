using Lagedra.Modules.Evidence.Application.DTOs;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Modules.Evidence.Application.Commands;

public sealed record RequestUploadUrlCommand(
    Guid ManifestId,
    string FileName,
    string MimeType) : IRequest<Result<UploadUrlDto>>;

public sealed class RequestUploadUrlCommandHandler
    : IRequestHandler<RequestUploadUrlCommand, Result<UploadUrlDto>>
{
    public Task<Result<UploadUrlDto>> Handle(
        RequestUploadUrlCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var storageKey = $"evidence/{request.ManifestId}/{Guid.NewGuid()}/{request.FileName}";
        var uploadId = Guid.NewGuid();

        var presignedUrl = $"/storage/upload/{storageKey}";
        var uri = new Uri(presignedUrl, UriKind.Absolute);

        var dto = new UploadUrlDto(uploadId, uri, storageKey);
        return Task.FromResult(Result<UploadUrlDto>.Success(dto));
    }
}
