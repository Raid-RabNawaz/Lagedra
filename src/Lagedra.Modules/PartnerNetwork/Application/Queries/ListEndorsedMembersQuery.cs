using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.Services;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

public sealed record EndorsedMemberDto(
    Guid TenantUserId,
    string DisplayName,
    string Email,
    Guid EndorsementId,
    DateTime? ApprovedAt);

public sealed record ListEndorsedMembersQuery(
    Guid OrganizationId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin) : IRequest<Result<IReadOnlyList<EndorsedMemberDto>>>;

public sealed class ListEndorsedMembersQueryHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IUserDirectoryService userDirectory)
    : IRequestHandler<ListEndorsedMembersQuery, Result<IReadOnlyList<EndorsedMemberDto>>>
{
    public async Task<Result<IReadOnlyList<EndorsedMemberDto>>> Handle(
        ListEndorsedMembersQuery request,
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
            return Result<IReadOnlyList<EndorsedMemberDto>>.Failure(authzResult.Error);
        }

        var endorsements = await dbContext.Endorsements
            .AsNoTracking()
            .Where(e => e.OrganizationId == request.OrganizationId
                     && e.Status == PartnerEndorsementStatus.Approved)
            .OrderByDescending(e => e.ApprovedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (endorsements.Count == 0)
        {
            return Result<IReadOnlyList<EndorsedMemberDto>>.Success(
                Array.Empty<EndorsedMemberDto>());
        }

        var identities = await PartnerUserIdentityResolver.ResolveAsync(
            dbContext,
            userDirectory,
            request.OrganizationId,
            endorsements.Select(e => e.TenantUserId).ToList(),
            cancellationToken).ConfigureAwait(false);

        var dtos = endorsements.Select(e =>
        {
            identities.TryGetValue(e.TenantUserId, out var identity);
            return new EndorsedMemberDto(
                e.TenantUserId,
                identity?.DisplayName ?? "Member",
                identity?.Email ?? string.Empty,
                e.Id,
                e.ApprovedAt);
        }).ToList().AsReadOnly();

        return Result<IReadOnlyList<EndorsedMemberDto>>.Success(dtos);
    }
}
