using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Authorization;

public sealed class PartnerAccessService(PartnerDbContext dbContext)
    : IPartnerAccessService
{
    public async Task<Result<PartnerAccess>> ResolveAsync(
        Guid callerUserId,
        Guid organizationId,
        bool isPlatformAdmin,
        CancellationToken ct = default)
    {
        var orgStatus = await dbContext.Organizations
            .AsNoTracking()
            .Where(o => o.Id == organizationId)
            .Select(o => (PartnerOrganizationStatus?)o.Status)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (orgStatus is null)
        {
            return Result<PartnerAccess>.Failure(PartnerAccessErrors.NotFound);
        }

        var member = await dbContext.Members
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId && m.UserId == callerUserId)
            .Select(m => (PartnerMemberRole?)m.MemberRole)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return Result<PartnerAccess>.Success(new PartnerAccess(
            OrganizationId: organizationId,
            IsPlatformAdmin: isPlatformAdmin,
            IsMember: member is not null,
            MemberRole: member,
            OrgStatus: orgStatus.Value));
    }

    public async Task<Result> RequireMemberAsync(
        Guid callerUserId,
        Guid organizationId,
        bool isPlatformAdmin,
        CancellationToken ct = default)
    {
        var resolveResult = await ResolveAsync(callerUserId, organizationId, isPlatformAdmin, ct)
            .ConfigureAwait(false);

        if (resolveResult.IsFailure)
        {
            return Result.Failure(resolveResult.Error);
        }

        return resolveResult.Value.CanRead
            ? Result.Success()
            : Result.Failure(PartnerAccessErrors.Forbidden);
    }

    public async Task<Result> RequireAdminMemberAsync(
        Guid callerUserId,
        Guid organizationId,
        bool isPlatformAdmin,
        CancellationToken ct = default)
    {
        var resolveResult = await ResolveAsync(callerUserId, organizationId, isPlatformAdmin, ct)
            .ConfigureAwait(false);

        if (resolveResult.IsFailure)
        {
            return Result.Failure(resolveResult.Error);
        }

        var access = resolveResult.Value;

        if (access.IsPlatformAdmin) return Result.Success();
        if (!access.IsMember) return Result.Failure(PartnerAccessErrors.Forbidden);
        if (!access.IsAdminMember) return Result.Failure(PartnerAccessErrors.AdminRequired);
        return Result.Success();
    }

    public async Task<Result> RequireVerifiedOrgAdminAsync(
        Guid callerUserId,
        Guid organizationId,
        bool isPlatformAdmin,
        CancellationToken ct = default)
    {
        var resolveResult = await ResolveAsync(callerUserId, organizationId, isPlatformAdmin, ct)
            .ConfigureAwait(false);

        if (resolveResult.IsFailure)
        {
            return Result.Failure(resolveResult.Error);
        }

        var access = resolveResult.Value;

        if (!access.IsPlatformAdmin && !access.IsMember)
        {
            return Result.Failure(PartnerAccessErrors.Forbidden);
        }

        if (!access.IsPlatformAdmin && !access.IsAdminMember)
        {
            return Result.Failure(PartnerAccessErrors.AdminRequired);
        }

        return access.OrgStatus switch
        {
            PartnerOrganizationStatus.Verified => Result.Success(),
            PartnerOrganizationStatus.Suspended => Result.Failure(PartnerAccessErrors.OrgSuspended),
            _ => Result.Failure(PartnerAccessErrors.OrgNotVerified)
        };
    }
}
