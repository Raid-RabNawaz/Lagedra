using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetCollectionsQuery(Guid UserId)
    : IRequest<Result<IReadOnlyList<SavedListingCollectionDto>>>;

public sealed class GetCollectionsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<GetCollectionsQuery, Result<IReadOnlyList<SavedListingCollectionDto>>>
{
    public async Task<Result<IReadOnlyList<SavedListingCollectionDto>>> Handle(
        GetCollectionsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var collections = await dbContext.SavedListingCollections
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name, c.CreatedAt })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var counts = await dbContext.SavedListings
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId && s.CollectionId != null)
            .GroupBy(s => s.CollectionId!.Value)
            .Select(g => new { CollectionId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var countMap = counts.ToDictionary(x => x.CollectionId, x => x.Count);

        var results = collections
            .Select(c => new SavedListingCollectionDto(
                c.Id,
                c.Name,
                c.CreatedAt,
                countMap.GetValueOrDefault(c.Id, 0)))
            .ToList();

        return Result<IReadOnlyList<SavedListingCollectionDto>>.Success(results);
    }
}
