using Lagedra.Infrastructure.External.Storage;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

/// <summary>
/// Removes the host's uploaded lease agreement, returning the listing to
/// Lagedra's standard lease.
/// </summary>
public sealed record RemoveListingLeaseDocumentCommand(Guid ListingId, Guid CallerUserId)
    : IRequest<Result<bool>>;

public sealed class RemoveListingLeaseDocumentCommandHandler(
    ListingsDbContext dbContext,
    IObjectStorageService storageService,
    IOptions<MinioSettings> storageOptions)
    : IRequestHandler<RemoveListingLeaseDocumentCommand, Result<bool>>
{
    private readonly string _bucket = storageOptions.Value.LeaseDocumentsBucket;

    public async Task<Result<bool>> Handle(
        RemoveListingLeaseDocumentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<bool>.Failure(new Error("Listing.NotFound", "Listing not found."));
        }

        if (listing.LandlordUserId != request.CallerUserId)
        {
            return Result<bool>.Failure(new Error("Listing.Forbidden", "You do not own this listing."));
        }

        var storageKey = listing.CustomLeaseDocument?.StorageKey;
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Result<bool>.Success(true);
        }

        listing.RemoveCustomLeaseDocument();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await storageService.DeleteObjectAsync(_bucket, storageKey, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // Ignored: the listing no longer references the object either way.
        }

        return Result<bool>.Success(true);
    }
}
