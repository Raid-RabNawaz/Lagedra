using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetListingPriceHistoryQuery(
    Guid ListingId,
    Guid? RequesterUserId = null,
    bool RequesterIsPlatformAdmin = false)
    : IRequest<Result<IReadOnlyList<ListingPriceHistoryDto>>>;

public sealed class GetListingPriceHistoryQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<GetListingPriceHistoryQuery, Result<IReadOnlyList<ListingPriceHistoryDto>>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<IReadOnlyList<ListingPriceHistoryDto>>> Handle(
        GetListingPriceHistoryQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listingMeta = await dbContext.Listings
            .AsNoTracking()
            .Where(l => l.Id == request.ListingId)
            .Select(l => new { l.LandlordUserId, l.Status })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (listingMeta is null)
        {
            return Result<IReadOnlyList<ListingPriceHistoryDto>>.Failure(NotFound);
        }

        // Same visibility gate as GetListingDetailsQuery.
        var isPubliclyVisible =
            listingMeta.Status == ListingStatus.Published ||
            listingMeta.Status == ListingStatus.Activated;
        var isOwner = request.RequesterUserId is Guid uid && uid == listingMeta.LandlordUserId;
        if (!isPubliclyVisible && !isOwner && !request.RequesterIsPlatformAdmin)
        {
            return Result<IReadOnlyList<ListingPriceHistoryDto>>.Failure(NotFound);
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
