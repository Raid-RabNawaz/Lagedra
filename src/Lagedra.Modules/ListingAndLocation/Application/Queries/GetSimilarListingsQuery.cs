using Lagedra.Modules.ListingAndLocation.Application.Commands;
using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetSimilarListingsQuery(Guid ListingId, int Limit = 6)
    : IRequest<Result<IReadOnlyList<ListingSummaryDto>>>;

public sealed class GetSimilarListingsQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<GetSimilarListingsQuery, Result<IReadOnlyList<ListingSummaryDto>>>
{
    private const double PriceTolerance = 0.20;

    public async Task<Result<IReadOnlyList<ListingSummaryDto>>> Handle(
        GetSimilarListingsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var source = await dbContext.Listings
            .AsNoTracking()
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (source is null)
        {
            return Result<IReadOnlyList<ListingSummaryDto>>.Failure(
                new Error("Listing.NotFound", "Listing not found."));
        }

        var minPrice = (long)(source.MonthlyRentCents * (1 - PriceTolerance));
        var maxPrice = (long)(source.MonthlyRentCents * (1 + PriceTolerance));

        var similar = await dbContext.Listings
            .AsNoTracking()
            .Include(l => l.Photos)
            .Where(l => l.Id != request.ListingId)
            .Where(l => l.Status == Domain.Enums.ListingStatus.Published || l.Status == Domain.Enums.ListingStatus.Activated)
            .Where(l => l.PropertyType == source.PropertyType)
            .Where(l => l.MonthlyRentCents >= minPrice && l.MonthlyRentCents <= maxPrice)
            .Where(l => source.JurisdictionCode == null || l.JurisdictionCode == source.JurisdictionCode)
            .OrderByDescending(l => l.CreatedAt)
            .Take(request.Limit)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = similar.Select(l => ListingMapper.ToSummary(l)).ToList();

        return Result<IReadOnlyList<ListingSummaryDto>>.Success(items);
    }
}
