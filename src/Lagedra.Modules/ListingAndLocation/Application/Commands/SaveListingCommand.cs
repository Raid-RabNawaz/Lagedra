using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record SaveListingCommand(
    Guid UserId,
    Guid ListingId) : IRequest<Result<SavedListingDto>>;

public sealed class SaveListingCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<SaveListingCommand, Result<SavedListingDto>>
{
    private static readonly Error ListingNotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error AlreadySaved = new("Listing.AlreadySaved", "Listing is already saved.");

    public async Task<Result<SavedListingDto>> Handle(
        SaveListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listingExists = await dbContext.Listings
            .AnyAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (!listingExists)
        {
            return Result<SavedListingDto>.Failure(ListingNotFound);
        }

        var alreadySaved = await dbContext.SavedListings
            .AnyAsync(s => s.UserId == request.UserId && s.ListingId == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (alreadySaved)
        {
            return Result<SavedListingDto>.Failure(AlreadySaved);
        }

        var saved = SavedListing.Create(request.UserId, request.ListingId);
        dbContext.SavedListings.Add(saved);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<SavedListingDto>.Success(new SavedListingDto(saved.ListingId, saved.SavedAt));
    }
}
