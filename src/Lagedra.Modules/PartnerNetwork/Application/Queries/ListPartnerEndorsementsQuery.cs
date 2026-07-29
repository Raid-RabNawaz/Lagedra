using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Application.Services;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

public sealed record ListPartnerEndorsementsQuery(
    Guid OrganizationId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin,
    PartnerEndorsementStatus? StatusFilter,
    int Skip,
    int Take) : IRequest<Result<IReadOnlyList<PartnerEndorsementDto>>>;

public sealed class ListPartnerEndorsementsQueryHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService,
    IUserDirectoryService userDirectory)
    : IRequestHandler<ListPartnerEndorsementsQuery, Result<IReadOnlyList<PartnerEndorsementDto>>>
{
    public async Task<Result<IReadOnlyList<PartnerEndorsementDto>>> Handle(
        ListPartnerEndorsementsQuery request,
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
            return Result<IReadOnlyList<PartnerEndorsementDto>>.Failure(authzResult.Error);
        }

        var orgName = await dbContext.Organizations
            .AsNoTracking()
            .Where(o => o.Id == request.OrganizationId)
            .Select(o => o.Name)
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        var query = dbContext.Endorsements
            .AsNoTracking()
            .Where(e => e.OrganizationId == request.OrganizationId);

        if (request.StatusFilter is { } status)
        {
            query = query.Where(e => e.Status == status);
        }

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take <= 0 ? 50 : request.Take, 1, 200);

        var rows = await query
            .OrderByDescending(e => e.RequestedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var identities = await PartnerUserIdentityResolver.ResolveAsync(
            dbContext,
            userDirectory,
            request.OrganizationId,
            rows.Select(e => e.TenantUserId).ToList(),
            cancellationToken).ConfigureAwait(false);

        var dtos = rows
            .Select(e =>
            {
                identities.TryGetValue(e.TenantUserId, out var identity);
                return EndorsementMapper.ToDto(e, orgName, identity?.DisplayName, identity?.Email);
            })
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<PartnerEndorsementDto>>.Success(dtos);
    }
}
