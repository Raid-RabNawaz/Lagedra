using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

/// <summary>
/// Public-ish (any authenticated user) lookup over <strong>verified</strong> partner
/// organizations only. Returns a minimal projection (id + name + type) so a tenant can
/// pick the correct partner to request an endorsement from without exposing internal
/// fields like contact email or tax id.
///
/// Distinct from <see cref="ListPartnerOrganizationsQuery"/>, which is admin-only and
/// returns the full DTO. The endpoint that calls this query enforces only the standard
/// auth (<c>RequireAuthorization()</c>); we do not need to check identity here.
/// </summary>
public sealed record DiscoverVerifiedPartnersQuery(string? Search, int Take)
    : IRequest<Result<IReadOnlyList<DiscoveredPartnerDto>>>;

public sealed record DiscoveredPartnerDto(
    Guid Id,
    string Name,
    PartnerOrganizationType OrganizationType);

public sealed class DiscoverVerifiedPartnersQueryHandler(PartnerDbContext dbContext)
    : IRequestHandler<DiscoverVerifiedPartnersQuery, Result<IReadOnlyList<DiscoveredPartnerDto>>>
{
    public async Task<Result<IReadOnlyList<DiscoveredPartnerDto>>> Handle(
        DiscoverVerifiedPartnersQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.Organizations
            .AsNoTracking()
            .Where(o => o.Status == PartnerOrganizationStatus.Verified);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search.Trim()}%";
            query = query.Where(o => EF.Functions.ILike(o.Name, term));
        }

        var take = Math.Clamp(request.Take <= 0 ? 25 : request.Take, 1, 50);

        var orgs = await query
            .OrderBy(o => o.Name)
            .Take(take)
            .Select(o => new DiscoveredPartnerDto(o.Id, o.Name, o.OrganizationType))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<DiscoveredPartnerDto>>.Success(orgs);
    }
}
