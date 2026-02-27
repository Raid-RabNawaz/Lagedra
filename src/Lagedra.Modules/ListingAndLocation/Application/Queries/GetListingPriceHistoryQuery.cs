using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetListingPriceHistoryQuery(Guid ListingId)
    : IRequest<Result<IReadOnlyList<ListingPriceHistoryDto>>>;

public sealed class GetListingPriceHistoryQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<GetListingPriceHistoryQuery, Result<IReadOnlyList<ListingPriceHistoryDto>>>
{
    public async Task<Result<IReadOnlyList<ListingPriceHistoryDto>>> Handle(
        GetListingPriceHistoryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var exists = await dbContext.Listings
            .AsNoTracking()
            .AnyAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            return Result<IReadOnlyList<ListingPriceHistoryDto>>.Failure(
                new Error("Listing.NotFound", "Listing not found."));
        }

        var history = await dbContext.ListingPriceHistory
            .AsNoTracking()
            .Where(h => h.ListingId == request.ListingId)
            .OrderBy(h => h.EffectiveFrom)
            .Select(h => new ListingPriceHistoryDto(h.Id, h.MonthlyRentCents, h.EffectiveFrom, h.EffectiveTo))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<ListingPriceHistoryDto>>.Success(history);
    }
}
