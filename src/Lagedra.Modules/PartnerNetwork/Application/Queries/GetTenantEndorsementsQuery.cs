using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

/// <summary>
/// Returns all endorsements (any status) for a tenant. Caller authorization:
/// the tenant themselves OR a platform admin.
/// </summary>
public sealed record GetTenantEndorsementsQuery(
    Guid TenantUserId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin) : IRequest<Result<IReadOnlyList<PartnerEndorsementDto>>>;

public sealed class GetTenantEndorsementsQueryHandler(PartnerDbContext dbContext)
    : IRequestHandler<GetTenantEndorsementsQuery, Result<IReadOnlyList<PartnerEndorsementDto>>>
{
    public async Task<Result<IReadOnlyList<PartnerEndorsementDto>>> Handle(
        GetTenantEndorsementsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.CallerIsPlatformAdmin && request.CallerUserId != request.TenantUserId)
        {
            return Result<IReadOnlyList<PartnerEndorsementDto>>.Failure(PartnerAccessErrors.Forbidden);
        }

        var rows = await dbContext.Endorsements
            .AsNoTracking()
            .Where(e => e.TenantUserId == request.TenantUserId)
            .OrderByDescending(e => e.RequestedAt)
            .Join(dbContext.Organizations.AsNoTracking(),
                e => e.OrganizationId,
                o => o.Id,
                (e, o) => new { Endorsement = e, OrgName = o.Name })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var dtos = rows
            .Select(r => EndorsementMapper.ToDto(r.Endorsement, r.OrgName))
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<PartnerEndorsementDto>>.Success(dtos);
    }
}
