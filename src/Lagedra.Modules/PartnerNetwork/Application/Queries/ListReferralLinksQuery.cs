using Lagedra.Modules.PartnerNetwork.Application.Authorization;
using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

public sealed record ListReferralLinksQuery(
    Guid OrganizationId,
    Guid CallerUserId,
    bool CallerIsPlatformAdmin) : IRequest<Result<IReadOnlyList<ReferralLinkDto>>>;

public sealed class ListReferralLinksQueryHandler(
    PartnerDbContext dbContext,
    IPartnerAccessService accessService)
    : IRequestHandler<ListReferralLinksQuery, Result<IReadOnlyList<ReferralLinkDto>>>
{
    public async Task<Result<IReadOnlyList<ReferralLinkDto>>> Handle(
        ListReferralLinksQuery request,
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
            return Result<IReadOnlyList<ReferralLinkDto>>.Failure(authzResult.Error);
        }

        var links = await dbContext.ReferralLinks
            .AsNoTracking()
            .Where(l => l.OrganizationId == request.OrganizationId)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new ReferralLinkDto(
                l.Id, l.OrganizationId, l.Code,
                l.CreatedByUserId, l.ExpiresAt, l.MaxUses,
                l.UsageCount, l.IsActive, l.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<ReferralLinkDto>>.Success(links);
    }
}
