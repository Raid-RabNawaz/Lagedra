using Lagedra.Modules.PartnerNetwork.Domain.Enums;

namespace Lagedra.Modules.PartnerNetwork.Presentation.Contracts;

public sealed record RegisterPartnerRequest(
    string Name,
    PartnerOrganizationType OrganizationType,
    string ContactEmail,
    string? TaxId);
