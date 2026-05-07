using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.SharedKernel.Results;

namespace Lagedra.Modules.PartnerNetwork.Application.Authorization;

/// <summary>
/// Centralised per-organization authorization for the PartnerNetwork module.
///
/// All <c>/v1/partners/{id}/*</c> endpoints (and the corresponding command / query handlers)
/// must call one of the <c>RequireXxx</c> helpers before doing any work. The service returns
/// structured <see cref="Result"/> failures so handlers stay free of <c>ClaimsPrincipal</c>
/// concerns and the endpoint layer can map directly to <c>403</c> / <c>404</c>.
///
/// The caller's <c>IsPlatformAdmin</c> flag is supplied by the endpoint after reading
/// the JWT role claim — handlers must never trust client-supplied admin flags.
/// </summary>
public interface IPartnerAccessService
{
    /// <summary>
    /// Returns the caller's resolved access against a given organization.
    /// Returns <c>Partner.NotFound</c> if the organization does not exist.
    /// </summary>
    Task<Result<PartnerAccess>> ResolveAsync(
        Guid callerUserId,
        Guid organizationId,
        bool isPlatformAdmin,
        CancellationToken ct = default);

    /// <summary>Caller must be a member of the org (any role) OR a platform admin.</summary>
    Task<Result> RequireMemberAsync(
        Guid callerUserId,
        Guid organizationId,
        bool isPlatformAdmin,
        CancellationToken ct = default);

    /// <summary>Caller must be an <see cref="PartnerMemberRole.Admin"/> member of the org OR a platform admin.</summary>
    Task<Result> RequireAdminMemberAsync(
        Guid callerUserId,
        Guid organizationId,
        bool isPlatformAdmin,
        CancellationToken ct = default);

    /// <summary>
    /// Caller must be an <see cref="PartnerMemberRole.Admin"/> member of the org (or a platform admin)
    /// AND the organization must be in <see cref="PartnerOrganizationStatus.Verified"/> status.
    /// </summary>
    Task<Result> RequireVerifiedOrgAdminAsync(
        Guid callerUserId,
        Guid organizationId,
        bool isPlatformAdmin,
        CancellationToken ct = default);
}

public sealed record PartnerAccess(
    Guid OrganizationId,
    bool IsPlatformAdmin,
    bool IsMember,
    PartnerMemberRole? MemberRole,
    PartnerOrganizationStatus OrgStatus)
{
    public bool IsAdminMember =>
        IsMember && MemberRole == PartnerMemberRole.Admin;

    public bool CanManage =>
        IsPlatformAdmin || IsAdminMember;

    public bool CanRead =>
        IsPlatformAdmin || IsMember;
}

public static class PartnerAccessErrors
{
    public static readonly Error NotFound =
        new("Partner.NotFound", "Partner organization not found.");

    public static readonly Error Forbidden =
        new("Partner.Forbidden", "You are not authorized to access this partner organization.");

    public static readonly Error AdminRequired =
        new("Partner.AdminRequired", "This action requires partner organization admin role.");

    public static readonly Error OrgNotVerified =
        new("Partner.OrgNotVerified", "Partner organization is not verified.");

    public static readonly Error OrgSuspended =
        new("Partner.OrgSuspended", "Partner organization is suspended.");
}
