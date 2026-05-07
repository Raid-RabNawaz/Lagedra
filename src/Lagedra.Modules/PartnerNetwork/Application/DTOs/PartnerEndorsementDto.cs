using Lagedra.Modules.PartnerNetwork.Domain.Enums;

namespace Lagedra.Modules.PartnerNetwork.Application.DTOs;

public sealed record PartnerEndorsementDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    Guid TenantUserId,
    PartnerEndorsementStatus Status,
    DateTime RequestedAt,
    Guid RequestedByUserId,
    DateTime? ApprovedAt,
    Guid? ApprovedByUserId,
    DateTime? RevokedAt,
    Guid? RevokedByUserId,
    string? RevokeReason,
    DateTime? ExpiresAt,
    string? Note);
