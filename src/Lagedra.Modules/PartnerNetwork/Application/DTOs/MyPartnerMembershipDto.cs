using Lagedra.Modules.PartnerNetwork.Domain.Enums;

namespace Lagedra.Modules.PartnerNetwork.Application.DTOs;

/// <summary>
/// Returned by <c>GET /v1/partners/me</c>. Wraps the caller's partner organization plus
/// their role within that organization, so the frontend can render the right nav and gate
/// admin-only actions without an additional round-trip.
/// </summary>
public sealed record MyPartnerMembershipDto(
    PartnerOrganizationDto Organization,
    PartnerMemberRole MemberRole,
    DateTime JoinedAt);
