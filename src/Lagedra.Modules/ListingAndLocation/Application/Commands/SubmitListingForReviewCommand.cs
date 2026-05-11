using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

/// <summary>
/// Landlord submits a Draft (or previously Denied) listing for admin
/// review. The listing transitions to <see cref="Domain.Enums.ListingStatus.InReview"/>
/// and only becomes publicly visible after an admin approves it.
/// </summary>
public sealed record SubmitListingForReviewCommand(
    Guid ListingId,
    Guid CallerUserId) : IRequest<Result<ListingDetailsDto>>;

public sealed class SubmitListingForReviewCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<SubmitListingForReviewCommand, Result<ListingDetailsDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error Forbidden = new("Listing.Forbidden", "You do not own this listing.");

    public async Task<Result<ListingDetailsDto>> Handle(
        SubmitListingForReviewCommand request,
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

        if (listing.LandlordUserId != request.CallerUserId)
        {
            return Result<ListingDetailsDto>.Failure(Forbidden);
        }

        try
        {
            listing.SubmitForReview();
        }
        catch (InvalidOperationException ex)
        {
            return Result<ListingDetailsDto>.Failure(new Error("Listing.SubmitForReviewFailed", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }
}
