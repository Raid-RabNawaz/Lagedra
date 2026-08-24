using Lagedra.Modules.PartnerNetwork.Domain.Enums;

namespace Lagedra.Modules.PartnerNetwork.Presentation.Contracts;

/// <summary>
/// Identify the member by email (preferred — users don't know their GUID) or
/// by user id (kept for admin tooling). At least one is required.
/// </summary>
public sealed record AddMemberRequest(
    PartnerMemberRole Role,
    Guid? UserId = null,
    string? Email = null);
