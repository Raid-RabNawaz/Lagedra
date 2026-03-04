using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record AddListingToCollectionCommand(
    Guid UserId,
    Guid ListingId,
    Guid CollectionId) : IRequest<Result>;

public sealed class AddListingToCollectionCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<AddListingToCollectionCommand, Result>
{
    private static readonly Error SavedListingNotFound = new(
        "SavedListing.NotFound",
        "Listing must be saved first. Save the listing, then add it to a collection.");
    private static readonly Error CollectionNotFound = new(
        "Collection.NotFound",
        "Collection not found or does not belong to you.");

    public async Task<Result> Handle(
        AddListingToCollectionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var collection = await dbContext.SavedListingCollections
            .FirstOrDefaultAsync(c => c.Id == request.CollectionId && c.UserId == request.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (collection is null)
        {
            return Result.Failure(CollectionNotFound);
        }

        var savedListing = await dbContext.SavedListings
            .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.ListingId == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (savedListing is null)
        {
            return Result.Failure(SavedListingNotFound);
        }

        savedListing.MoveToCollection(request.CollectionId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
