namespace Lagedra.Modules.Evidence.Application.DTOs;

public sealed record UploadUrlDto(
    Guid UploadId,
    Uri PresignedUrl,
    string StorageKey);
