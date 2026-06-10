using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Queries;

/// <summary>
/// Returns availability for a listing. When <paramref name="From"/> and
/// <paramref name="To"/> are supplied, the response is range-scoped:
/// <see cref="ListingAvailabilityDto.Available"/> reflects whether any
/// blocking exists in the requested window and
/// <see cref="ListingAvailabilityDto.Blocks"/> contains only the blocks
/// that overlap it. Without a window, the full block list is returned and
/// <see cref="ListingAvailabilityDto.Available"/> is <c>true</c>.
/// </summary>
public sealed record GetListingAvailabilityQuery(
    Guid ListingId,
    Guid? RequesterUserId = null,
    bool RequesterIsPlatformAdmin = false,
    DateOnly? From = null,
    DateOnly? To = null)
    : IRequest<Result<ListingAvailabilityDto>>;

public sealed class GetListingAvailabilityQueryHandler(ListingsDbContext dbContext)
    : IRequestHandler<GetListingAvailabilityQuery, Result<ListingAvailabilityDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error InvalidRange = new(
        "Listing.Availability.InvalidRange",
        "'to' must be strictly after 'from'.");

    public async Task<Result<ListingAvailabilityDto>> Handle(
        GetListingAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.From is { } from && request.To is { } to && to <= from)
        {
            return Result<ListingAvailabilityDto>.Failure(InvalidRange);
        }

        var listingMeta = await dbContext.Listings
            .AsNoTracking()
            .Where(l => l.Id == request.ListingId)
            .Select(l => new { l.LandlordUserId, l.Status })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (listingMeta is null)
        {
            return Result<ListingAvailabilityDto>.Failure(NotFound);
        }

        // Same visibility gate as GetListingDetailsQuery.
        var isPubliclyVisible =
            listingMeta.Status == ListingStatus.Published ||
            listingMeta.Status == ListingStatus.Activated;
        var isOwner = request.RequesterUserId is Guid uid && uid == listingMeta.LandlordUserId;
        if (!isPubliclyVisible && !isOwner && !request.RequesterIsPlatformAdmin)
        {
            return Result<ListingAvailabilityDto>.Failure(NotFound);
        }

        var blocksQuery = dbContext.ListingAvailabilityBlocks
            .AsNoTracking()
            .Where(b => b.ListingId == request.ListingId);

        if (request.From is { } rangeFrom && request.To is { } rangeTo)
        {
            // Half-open overlap test: block (start, end) overlaps [from, to)
            // iff start < to AND end > from.
            blocksQuery = blocksQuery.Where(b =>
                b.CheckInDate < rangeTo && b.CheckOutDate > rangeFrom);
        }

        var blocks = await blocksQuery
            .OrderBy(b => b.CheckInDate)
            .Select(b => new AvailabilityBlockDto(b.Id, b.CheckInDate, b.CheckOutDate, b.BlockType))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // When the caller supplied a window, "available" means no overlapping
        // blocks. Without a window we don't compute availability — the legacy
        // calendar UI just wants the raw blocks.
        var available = !(request.From.HasValue && request.To.HasValue && blocks.Count > 0);

        return Result<ListingAvailabilityDto>.Success(
            new ListingAvailabilityDto(available, blocks));
    }
}
