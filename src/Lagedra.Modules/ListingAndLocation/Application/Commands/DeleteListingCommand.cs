using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

/// <summary>
/// Landlord deletes a listing. Permitted only when the listing is <c>Draft</c>
/// or <c>Denied</c>. Once a listing has been Published/Activated/Closed it
/// must remain in the system so historical data (deals, audit, payments)
/// stays intact — the landlord should close instead.
/// </summary>
public sealed record DeleteListingCommand(
    Guid ListingId,
    Guid CallerUserId) : IRequest<Result>;

public sealed class DeleteListingCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<DeleteListingCommand, Result>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error Forbidden = new("Listing.Forbidden", "You do not own this listing.");

    public async Task<Result> Handle(
        DeleteListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
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

        try
        {
            listing.DeleteByLandlord();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(new Error("Listing.DeleteFailed", ex.Message));
        }

        dbContext.Listings.Remove(listing);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
