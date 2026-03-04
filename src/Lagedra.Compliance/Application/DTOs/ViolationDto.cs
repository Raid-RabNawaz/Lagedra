using Lagedra.Compliance.Domain;

namespace Lagedra.Compliance.Application.DTOs;

public sealed record ViolationDto(
    Guid Id,
    Guid DealId,
    Guid ReportedByUserId,
    Guid TargetUserId,
    ViolationCategory Category,
    ViolationStatus Status,
    string Description,
    string? EvidenceReference,
    DateTime DetectedAt,
    DateTime? ResolvedAt);
