using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Queries;

/// <summary>
/// Admin-only listing of partner organizations, optionally filtered by status.
/// Authorization is enforced at the endpoint via the <c>RequirePlatformAdmin</c> policy;
/// this handler does not double-check.
/// </summary>
public sealed record ListPartnerOrganizationsQuery(
    PartnerOrganizationStatus? StatusFilter,
    string? Search,
    int Skip,
    int Take) : IRequest<Result<IReadOnlyList<PartnerOrganizationDto>>>;

public sealed class ListPartnerOrganizationsQueryHandler(PartnerDbContext dbContext)
    : IRequestHandler<ListPartnerOrganizationsQuery, Result<IReadOnlyList<PartnerOrganizationDto>>>
{
    public async Task<Result<IReadOnlyList<PartnerOrganizationDto>>> Handle(
        ListPartnerOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.Organizations.AsNoTracking();

        if (request.StatusFilter is { } status)
        {
            query = query.Where(o => o.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search.Trim()}%";
            query = query.Where(o =>
                EF.Functions.ILike(o.Name, term)
                || EF.Functions.ILike(o.ContactEmail, term)
                || (o.TaxId != null && EF.Functions.ILike(o.TaxId, term)));
        }

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take <= 0 ? 50 : request.Take, 1, 200);

        var orgs = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(o => new PartnerOrganizationDto(
                o.Id, o.Name, o.OrganizationType, o.Status, o.ContactEmail,
                o.TaxId, o.VerifiedAt, o.CreatedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<PartnerOrganizationDto>>.Success(orgs);
    }
}
