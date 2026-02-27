using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

public sealed record GetListingAvailabilityQuery(Guid ListingId)
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

        var exists = await dbContext.Listings
            .AnyAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
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
