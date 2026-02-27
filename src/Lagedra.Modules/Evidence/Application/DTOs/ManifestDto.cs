using Lagedra.Modules.Evidence.Domain.Enums;

namespace Lagedra.Modules.Evidence.Application.DTOs;

public sealed record ManifestDto(
    Guid ManifestId,
    Guid DealId,
    ManifestType ManifestType,
    ManifestStatus Status,
    DateTime CreatedAt,
    DateTime? SealedAt,
    string? HashOfAllFiles,
    IReadOnlyList<ManifestUploadDto> Uploads);

public sealed record ManifestUploadDto(
    Guid UploadId,
    string OriginalFileName,
    string MimeType,
    string? FileHash,
    DateTime UploadedAt);
