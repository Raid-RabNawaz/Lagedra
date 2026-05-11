using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record ReorderPhotosCommand(
    Guid ListingId,
    Guid CallerUserId,
    IReadOnlyList<Guid> PhotoIdsInOrder) : IRequest<Result>;

public sealed class ReorderPhotosCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<ReorderPhotosCommand, Result>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error Forbidden = new("Listing.Forbidden", "You do not own this listing.");

    public async Task<Result> Handle(
        ReorderPhotosCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result.Failure(NotFound);
        }

        if (listing.LandlordUserId != request.CallerUserId)
        {
            return Result.Failure(Forbidden);
        }

        listing.ReorderPhotos(request.PhotoIdsInOrder);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
