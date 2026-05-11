using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record SetCoverPhotoCommand(
    Guid ListingId,
    Guid CallerUserId,
    Guid PhotoId) : IRequest<Result>;

public sealed class SetCoverPhotoCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<SetCoverPhotoCommand, Result>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error Forbidden = new("Listing.Forbidden", "You do not own this listing.");

    public async Task<Result> Handle(
        SetCoverPhotoCommand request,
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

        listing.SetCoverPhoto(request.PhotoId);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
