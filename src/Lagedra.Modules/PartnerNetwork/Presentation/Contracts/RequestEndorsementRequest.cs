namespace Lagedra.Modules.PartnerNetwork.Presentation.Contracts;

public sealed record RequestEndorsementRequest(Guid TenantUserId, string? Note);

public sealed record RequestEndorsementByTenantRequest(Guid OrganizationId, string? Note);

public sealed record ApproveEndorsementRequest(string? Note);

public sealed record RevokeEndorsementRequest(string Reason);

public sealed record InvitePartnerGuestRequest(
    string Email,
    string FullName,
    bool WithEndorsement,
    string? EndorsementNote);
