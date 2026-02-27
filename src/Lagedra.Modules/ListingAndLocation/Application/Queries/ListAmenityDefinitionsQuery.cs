using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record ListAmenityDefinitionsQuery(bool ActiveOnly = true)
    : IRequest<Result<IReadOnlyList<AmenityDefinitionDto>>>;

public sealed class ListAmenityDefinitionsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<ListAmenityDefinitionsQuery, Result<IReadOnlyList<AmenityDefinitionDto>>>
{
    public async Task<Result<IReadOnlyList<AmenityDefinitionDto>>> Handle(
        ListAmenityDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.AmenityDefinitions.AsNoTracking();

        if (request.ActiveOnly)
        {
            query = query.Where(a => a.IsActive);
        }

        var definitions = await query
            .OrderBy(a => a.Category).ThenBy(a => a.SortOrder)
            .Select(a => new AmenityDefinitionDto(a.Id, a.Name, a.Category, a.IconKey))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<AmenityDefinitionDto>>.Success(definitions);
    }
}
