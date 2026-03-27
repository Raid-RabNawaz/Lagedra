using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetMyListingsQuery(Guid LandlordUserId) : IRequest<Result<IReadOnlyList<ListingSummaryDto>>>;

public sealed class GetMyListingsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<GetMyListingsQuery, Result<IReadOnlyList<ListingSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<ListingSummaryDto>>> Handle(
        GetMyListingsQuery request,
        CancellationToken cancellationToken)
    {
        var listings = await dbContext.Listings
            .AsNoTracking()
            .Include(l => l.Photos)
            .Where(l => l.LandlordUserId == request.LandlordUserId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = listings.Select(l => ListingMapper.ToSummary(l)).ToList();
        return Result<IReadOnlyList<ListingSummaryDto>>.Success(items);
    }
}
