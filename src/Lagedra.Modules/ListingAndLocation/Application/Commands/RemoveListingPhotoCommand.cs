using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record RemoveListingPhotoCommand(
    Guid ListingId,
    Guid PhotoId) : IRequest<Result>;

public sealed class RemoveListingPhotoCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<RemoveListingPhotoCommand, Result>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result> Handle(
        RemoveListingPhotoCommand request,
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

        listing.RemovePhoto(request.PhotoId);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
