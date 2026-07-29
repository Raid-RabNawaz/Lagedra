using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Application.Services;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

public sealed record ListPartnerMembersQuery(
    Guid OrganizationId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin) : IRequest<Result<IReadOnlyList<PartnerMemberDto>>>;

public sealed class ListPartnerMembersQueryHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IUserDirectoryService userDirectory)
    : IRequestHandler<ListPartnerMembersQuery, Result<IReadOnlyList<PartnerMemberDto>>>
{
    public async Task<Result<IReadOnlyList<PartnerMemberDto>>> Handle(
        ListPartnerMembersQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var authzResult = await accessService.RequireMemberAsync(
            request.CallerUserId,
            request.OrganizationId,
            request.CallerIsPlatformAdmin,
            cancellationToken).ConfigureAwait(false);

        if (authzResult.IsFailure)
        {
            return Result<IReadOnlyList<PartnerMemberDto>>.Failure(authzResult.Error);
        }

        var members = await dbContext.Members
            .AsNoTracking()
            .Where(m => m.OrganizationId == request.OrganizationId)
            .OrderByDescending(m => m.JoinedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var userIds = members.Select(m => m.UserId)
            .Concat(members.Where(m => m.InvitedBy.HasValue).Select(m => m.InvitedBy!.Value))
            .ToList();

        var identities = await PartnerUserIdentityResolver.ResolveAsync(
            dbContext,
            userDirectory,
            request.OrganizationId,
            userIds,
            cancellationToken).ConfigureAwait(false);

        var dtos = members
            .Select(m =>
            {
                identities.TryGetValue(m.UserId, out var identity);
                ResolvedUserIdentity? inviter = null;
                if (m.InvitedBy is { } invitedBy)
                {
                    identities.TryGetValue(invitedBy, out inviter);
                }

                return new PartnerMemberDto(
                    m.Id, m.OrganizationId, m.UserId,
                    m.MemberRole, m.JoinedAt, m.InvitedBy,
                    identity?.DisplayName,
                    identity?.Email,
                    inviter?.DisplayName);
            })
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<PartnerMemberDto>>.Success(dtos);
    }
}
