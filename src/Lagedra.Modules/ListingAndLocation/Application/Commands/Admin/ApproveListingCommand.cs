using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands.Admin;

/// <summary>
/// Admin approves a listing currently <c>InReview</c>, moving it to
/// <c>Published</c> so tenants can discover it in search.
/// </summary>
public sealed record ApproveListingCommand(
    Guid ListingId,
    Guid AdminUserId) : IRequest<Result<ListingDetailsDto>>;

public sealed class ApproveListingCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<ApproveListingCommand, Result<ListingDetailsDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");

    public async Task<Result<ListingDetailsDto>> Handle(
        ApproveListingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Status transition only — do not eager-load photos/amenities. Hostaway
        // imports can have hundreds of photos; that graph routinely exceeds the
        // SPA's default axios timeout and surfaces as a fake "network" error.
        var listing = await dbContext.Listings
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<ListingDetailsDto>.Failure(NotFound);
        }

        try
        {
            listing.ApproveByAdmin(request.AdminUserId);
        }
        catch (InvalidOperationException ex)
        {
            return Result<ListingDetailsDto>.Failure(new Error("Listing.ApproveFailed", ex.Message));
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ListingDetailsDto>.Success(ListingMapper.ToDetails(listing));
    }
}
