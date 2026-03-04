using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record RemoveListingFromCollectionCommand(Guid UserId, Guid ListingId) : IRequest<Result>;

public sealed class RemoveListingFromCollectionCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<RemoveListingFromCollectionCommand, Result>
{
    public async Task<Result> Handle(
        RemoveListingFromCollectionCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var savedListing = await dbContext.SavedListings
            .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.ListingId == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (savedListing is null)
        {
            return Result.Failure(new Error("SavedListing.NotFound", "Saved listing not found."));
        }

        savedListing.MoveToCollection(null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
