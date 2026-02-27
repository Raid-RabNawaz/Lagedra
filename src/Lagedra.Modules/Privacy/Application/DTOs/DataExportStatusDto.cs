using Lagedra.Modules.Privacy.Domain.Enums;

namespace Lagedra.Modules.Privacy.Application.DTOs;

public sealed record DataExportStatusDto(
    Guid Id,
    Guid UserId,
    ExportStatus Status,
    DateTime RequestedAt,
    DateTime? CompletedAt,
    Uri? PackageUrl);
