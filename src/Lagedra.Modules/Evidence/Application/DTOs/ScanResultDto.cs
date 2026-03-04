using Lagedra.Modules.Evidence.Domain.Enums;

namespace Lagedra.Modules.Evidence.Application.DTOs;

public sealed record ScanResultDto(
    Guid UploadId,
    ScanStatus Status,
    DateTime? ScannedAt);
