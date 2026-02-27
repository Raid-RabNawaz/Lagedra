using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record ListSafetyDeviceDefinitionsQuery(bool ActiveOnly = true)
    : IRequest<Result<IReadOnlyList<SafetyDeviceDefinitionDto>>>;

public sealed class ListSafetyDeviceDefinitionsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<ListSafetyDeviceDefinitionsQuery, Result<IReadOnlyList<SafetyDeviceDefinitionDto>>>
{
    public async Task<Result<IReadOnlyList<SafetyDeviceDefinitionDto>>> Handle(
        ListSafetyDeviceDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.SafetyDeviceDefinitions.AsNoTracking();

        if (request.ActiveOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var definitions = await query
            .OrderBy(s => s.SortOrder)
            .Select(s => new SafetyDeviceDefinitionDto(s.Id, s.Name, s.IconKey))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<SafetyDeviceDefinitionDto>>.Success(definitions);
    }
}
