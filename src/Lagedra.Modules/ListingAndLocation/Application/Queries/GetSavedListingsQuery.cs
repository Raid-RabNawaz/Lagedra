using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetSavedListingsQuery(
    Guid UserId,
    Guid? CollectionId = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<IReadOnlyList<ListingSummaryDto>>>;

public sealed class GetSavedListingsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<GetSavedListingsQuery, Result<IReadOnlyList<ListingSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<ListingSummaryDto>>> Handle(
        GetSavedListingsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = dbContext.SavedListings
            .AsNoTracking()
            .Where(s => s.UserId == request.UserId);

        if (request.CollectionId.HasValue)
        {
            query = query.Where(s => s.CollectionId == request.CollectionId.Value);
        }

        var savedListingIds = await query
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

        IReadOnlyList<ListingSummaryDto> results = savedListingIds
            .Select(id => listings.FirstOrDefault(l => l.Id == id))
            .Where(l => l is not null)
            .Select(l => ListingMapper.ToSummary(l!))
            .ToList();

        return Result<IReadOnlyList<ListingSummaryDto>>.Success(results);
    }
}
