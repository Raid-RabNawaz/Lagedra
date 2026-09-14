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
    DateTime? ResolvedAt,
    string? TargetEmail = null,
    string? TargetName = null,
    string? ReportedByEmail = null,
    string? ReportedByName = null,
    Guid? ListingId = null,
    string? ListingTitle = null);
