using Lagedra.Modules.PartnerNetwork.Domain.Enums;

namespace Lagedra.Modules.PartnerNetwork.Application.DTOs;

public sealed record PartnerOrganizationDto(
    Guid Id,
    string Name,
    PartnerOrganizationType OrganizationType,
    PartnerOrganizationStatus Status,
    string ContactEmail,
    string? TaxId,
    DateTime? VerifiedAt,
    DateTime CreatedAt);
