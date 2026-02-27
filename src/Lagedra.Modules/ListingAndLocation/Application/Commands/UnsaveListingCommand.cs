using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record UnsaveListingCommand(
    Guid UserId,
    Guid ListingId) : IRequest<Result>;

public sealed class UnsaveListingCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<UnsaveListingCommand, Result>
{
    private static readonly Error NotFound = new("SavedListing.NotFound", "Saved listing not found.");

    public async Task<Result> Handle(
        UnsaveListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var saved = await dbContext.SavedListings
            .FirstOrDefaultAsync(s => s.UserId == request.UserId && s.ListingId == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (saved is null)
        {
            return Result.Failure(NotFound);
        }

        dbContext.SavedListings.Remove(saved);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
