using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record ListConsiderationDefinitionsQuery(bool ActiveOnly = true)
    : IRequest<Result<IReadOnlyList<ConsiderationDefinitionDto>>>;

public sealed class ListConsiderationDefinitionsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<ListConsiderationDefinitionsQuery, Result<IReadOnlyList<ConsiderationDefinitionDto>>>
{
    public async Task<Result<IReadOnlyList<ConsiderationDefinitionDto>>> Handle(
        ListConsiderationDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.ConsiderationDefinitions.AsNoTracking();

        if (request.ActiveOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        var definitions = await query
            .OrderBy(c => c.SortOrder)
            .Select(c => new ConsiderationDefinitionDto(c.Id, c.Name, c.IconKey))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<ConsiderationDefinitionDto>>.Success(definitions);
    }
}
