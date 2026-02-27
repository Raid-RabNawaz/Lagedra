using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetCollectionListingsQuery(
    Guid UserId,
    Guid CollectionId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<IReadOnlyList<ListingSummaryDto>>>;

public sealed class GetCollectionListingsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<GetCollectionListingsQuery, Result<IReadOnlyList<ListingSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<ListingSummaryDto>>> Handle(
        GetCollectionListingsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var collectionExists = await dbContext.SavedListingCollections
            .AsNoTracking()
            .AnyAsync(c => c.Id == request.CollectionId && c.UserId == request.UserId && !c.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        if (!collectionExists)
        {
            return Result<IReadOnlyList<ListingSummaryDto>>.Failure(
                new Error("Collection.NotFound", "Collection not found."));
        }

        var savedListingIds = await dbContext.SavedListings
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId && s.CollectionId == request.CollectionId)
            .OrderByDescending(s => s.SavedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => s.ListingId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var listings = await dbContext.Listings
            .AsNoTracking()
            .Include(l => l.Photos)
            .Where(l => savedListingIds.Contains(l.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var results = savedListingIds
            .Select(id => listings.FirstOrDefault(l => l.Id == id))
            .Where(l => l is not null)
            .Select(l => ListingMapper.ToSummary(l!))
            .ToList();

        return Result<IReadOnlyList<ListingSummaryDto>>.Success(results);
    }
}
