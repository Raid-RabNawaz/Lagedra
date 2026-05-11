using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;

namespace Lagedra.Modules.PartnerNetwork.Application.DTOs;

internal static class EndorsementMapper
{
    public static PartnerEndorsementDto ToDto(PartnerEndorsement e, string organizationName) =>
        new(e.Id,
            e.OrganizationId,
            organizationName,
            e.TenantUserId,
            e.Status,
            e.RequestedAt,
            e.RequestedByUserId,
            e.ApprovedAt,
            e.ApprovedByUserId,
            e.RevokedAt,
            e.RevokedByUserId,
            e.RevokeReason,
            e.ExpiresAt,
            e.Note);
}
