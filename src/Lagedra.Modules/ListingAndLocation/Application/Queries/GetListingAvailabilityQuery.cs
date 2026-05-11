using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetListingAvailabilityQuery(
    Guid ListingId,
    Guid? RequesterUserId = null,
    bool RequesterIsPlatformAdmin = false)
    : IRequest<Result<IReadOnlyList<AvailabilityBlockDto>>>;

public sealed class GetListingAvailabilityQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<GetListingAvailabilityQuery, Result<IReadOnlyList<AvailabilityBlockDto>>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<IReadOnlyList<AvailabilityBlockDto>>> Handle(
        GetListingAvailabilityQuery request,
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
            return Result<IReadOnlyList<AvailabilityBlockDto>>.Failure(NotFound);
        }

        // Same visibility gate as GetListingDetailsQuery.
        var isPubliclyVisible =
            listingMeta.Status == ListingStatus.Published ||
            listingMeta.Status == ListingStatus.Activated;
        var isOwner = request.RequesterUserId is Guid uid && uid == listingMeta.LandlordUserId;
        if (!isPubliclyVisible && !isOwner && !request.RequesterIsPlatformAdmin)
        {
            return Result<IReadOnlyList<AvailabilityBlockDto>>.Failure(NotFound);
        }

        var blocks = await dbContext.ListingAvailabilityBlocks
            .AsNoTracking()
            .Where(b => b.ListingId == request.ListingId)
            .OrderBy(b => b.CheckInDate)
            .Select(b => new AvailabilityBlockDto(b.Id, b.CheckInDate, b.CheckOutDate, b.BlockType))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<AvailabilityBlockDto>>.Success(blocks);
    }
}
