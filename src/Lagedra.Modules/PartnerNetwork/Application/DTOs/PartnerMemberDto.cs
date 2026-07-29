using Lagedra.Modules.PartnerNetwork.Domain.Enums;

namespace Lagedra.Modules.PartnerNetwork.Application.DTOs;

public sealed record PartnerMemberDto(
    Guid Id,
    Guid OrganizationId,
    Guid UserId,
    PartnerMemberRole MemberRole,
    DateTime JoinedAt,
    Guid? InvitedBy,
    string? DisplayName = null,
    string? Email = null,
    string? InvitedByDisplayName = null);
