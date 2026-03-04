using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record AddListingPhotoCommand(
    Guid ListingId,
    string StorageKey,
    Uri Url,
    string? Caption) : IRequest<Result<ListingPhotoDto>>;

public sealed class AddListingPhotoCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<AddListingPhotoCommand, Result<ListingPhotoDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<ListingPhotoDto>> Handle(
        AddListingPhotoCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<ListingPhotoDto>.Failure(NotFound);
        }

        var photo = listing.AddPhoto(request.StorageKey, request.Url, request.Caption);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingPhotoDto>.Success(
            new ListingPhotoDto(photo.Id, photo.Url, photo.Caption, photo.IsCover, photo.SortOrder));
    }
}
