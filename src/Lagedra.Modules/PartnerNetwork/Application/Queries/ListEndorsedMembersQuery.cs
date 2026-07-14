using Lagedra.Modules.PartnerNetwork.Application.Authorization;
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

        var tenantIds = endorsements.Select(e => e.TenantUserId).Distinct().ToList();
        var directory = await userDirectory
            .GetEntriesAsync(tenantIds, cancellationToken)
            .ConfigureAwait(false);

        // Prefer invite audit name/email when the directory entry is sparse.
        var invites = await dbContext.GuestInvites
            .AsNoTracking()
            .Where(i => i.OrganizationId == request.OrganizationId
                     && tenantIds.Contains(i.InvitedUserId))
            .OrderByDescending(i => i.InvitedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var latestInviteByUser = invites
            .GroupBy(i => i.InvitedUserId)
            .ToDictionary(g => g.Key, g => g.First());

        var dtos = endorsements.Select(e =>
        {
            directory.TryGetValue(e.TenantUserId, out var entry);
            latestInviteByUser.TryGetValue(e.TenantUserId, out var invite);

            var email = !string.IsNullOrWhiteSpace(entry?.Email)
                ? entry!.Email
                : invite?.Email ?? string.Empty;
            var displayName = !string.IsNullOrWhiteSpace(entry?.DisplayName)
                && entry!.DisplayName != email
                ? entry.DisplayName
                : !string.IsNullOrWhiteSpace(invite?.FullName)
                    ? invite!.FullName
                    : !string.IsNullOrWhiteSpace(entry?.DisplayName)
                        ? entry!.DisplayName
                        : !string.IsNullOrWhiteSpace(email)
                            ? email
                            : "Member";

            return new EndorsedMemberDto(
                e.TenantUserId,
                displayName,
                email,
                e.Id,
                e.ApprovedAt);
        }).ToList().AsReadOnly();

        return Result<IReadOnlyList<EndorsedMemberDto>>.Success(dtos);
    }
}
