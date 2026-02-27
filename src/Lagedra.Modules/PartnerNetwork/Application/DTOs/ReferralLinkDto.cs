namespace Lagedra.Modules.PartnerNetwork.Application.DTOs;

public sealed record ReferralLinkDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    Guid CreatedByUserId,
    DateTime? ExpiresAt,
    int? MaxUses,
    int UsageCount,
    bool IsActive,
    DateTime CreatedAt);
