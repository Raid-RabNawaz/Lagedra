using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

/// <summary>
/// Removes a member from a partner organization. Only org admins (or platform
/// admins) may remove members, and the organization must always keep at least
/// one admin so it never becomes unmanageable.
/// </summary>
public sealed record RemovePartnerMemberCommand(
    Guid OrganizationId,
    Guid MemberId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin) : IRequest<Result>;

public sealed class RemovePartnerMemberCommandHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService)
    : IRequestHandler<RemovePartnerMemberCommand, Result>
{
    public async Task<Result> Handle(
        RemovePartnerMemberCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authzResult = await accessService.RequireAdminMemberAsync(
            request.CallerUserId,
            request.OrganizationId,
            request.CallerIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result.Failure(authzResult.Error);
        }

        var member = await dbContext.Members
            .FirstOrDefaultAsync(
                m => m.Id == request.MemberId && m.OrganizationId == request.OrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (member is null)
        {
            return Result.Failure(
                new Error("Partner.MemberNotFound", "Member not found in this organization."));
        }

        if (member.MemberRole == PartnerMemberRole.Admin)
        {
            var adminCount = await dbContext.Members
                .CountAsync(
                    m => m.OrganizationId == request.OrganizationId
                      && m.MemberRole == PartnerMemberRole.Admin,
                    cancellationToken)
                .ConfigureAwait(false);

            if (adminCount <= 1)
            {
                return Result.Failure(
                    new Error("Partner.LastAdmin",
                        "An organization must keep at least one admin. Promote another member first."));
            }
        }

        dbContext.Members.Remove(member);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
