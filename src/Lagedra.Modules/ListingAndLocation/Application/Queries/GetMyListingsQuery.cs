using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetMyListingsQuery(Guid LandlordUserId) : IRequest<Result<IReadOnlyList<ListingSummaryDto>>>;

public sealed class GetMyListingsQueryHandler(
    ListingsDbContext dbContext,
    IServiceProvider serviceProvider)
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

        var reputationProvider = serviceProvider.GetService<IReviewReputationProvider>();
        var items = await ListingSummaryEnricher
            .ToSummariesAsync(listings, reputationProvider, cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<ListingSummaryDto>>.Success(items);
    }
}
