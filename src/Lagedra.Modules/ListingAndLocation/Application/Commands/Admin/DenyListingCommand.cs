using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

/// <summary>
/// Admin denies a listing currently <c>InReview</c>, attaching a reason that
/// the landlord can read so they know what to fix before resubmitting.
/// </summary>
public sealed record DenyListingCommand(
    Guid ListingId,
    Guid AdminUserId,
    string Reason) : IRequest<Result<ListingDetailsDto>>;

public sealed class DenyListingCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<DenyListingCommand, Result<ListingDetailsDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error ReasonRequired = new(
        "Listing.DenyReasonRequired",
        "A rejection reason is required so the landlord knows what to fix.");

    public async Task<Result<ListingDetailsDto>> Handle(
        DenyListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return Result<ListingDetailsDto>.Failure(ReasonRequired);
        }

        // Status transition only — skip photo/amenity graph (same timeout risk as approve).
        var listing = await dbContext.Listings
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<ListingDetailsDto>.Failure(NotFound);
        }

        try
        {
            listing.DenyByAdmin(request.AdminUserId, request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ListingDetailsDto>.Failure(new Error("Listing.DenyFailed", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }
}
