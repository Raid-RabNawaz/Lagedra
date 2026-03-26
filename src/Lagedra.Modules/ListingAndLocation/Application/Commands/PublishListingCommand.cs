using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record PublishListingCommand(Guid ListingId) : IRequest<Result<ListingDetailsDto>>;

public sealed class PublishListingCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<PublishListingCommand, Result<ListingDetailsDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<ListingDetailsDto>> Handle(
        PublishListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .Include(l => l.Amenities).ThenInclude(a => a.AmenityDefinition)
            .Include(l => l.SafetyDevices).ThenInclude(s => s.SafetyDeviceDefinition)
            .Include(l => l.Considerations).ThenInclude(c => c.ConsiderationDefinition)
            .Include(l => l.Photos)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<ListingDetailsDto>.Failure(NotFound);
        }

        try
        {
            listing.Publish();
        }
        catch (InvalidOperationException ex)
        {
            return Result<ListingDetailsDto>.Failure(new Error("Listing.PublishFailed", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }
}
