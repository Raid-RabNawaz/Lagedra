using Lagedra.Modules.ListingAndLocation.Application.DTOs;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Services;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record BlockDatesCommand(
    Guid ListingId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate) : IRequest<Result<AvailabilityBlockDto>>;

public sealed class BlockDatesCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<BlockDatesCommand, Result<AvailabilityBlockDto>>
{
    private static readonly Error NotFound = new("Listing.NotFound", "Listing not found.");
    private static readonly Error Conflict = new("Dates.Conflict", "The requested dates overlap with an existing block.");

    public async Task<Result<AvailabilityBlockDto>> Handle(
        BlockDatesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var listing = await dbContext.Listings
            .Include(l => l.AvailabilityBlocks)
            .FirstOrDefaultAsync(l => l.Id == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (listing is null)
        {
            return Result<AvailabilityBlockDto>.Failure(NotFound);
        }

        if (!AvailabilityService.IsAvailable(listing.AvailabilityBlocks, request.CheckInDate, request.CheckOutDate))
        {
            return Result<AvailabilityBlockDto>.Failure(Conflict);
        }

        var block = ListingAvailabilityBlock.CreateHostBlocked(
            request.ListingId, request.CheckInDate, request.CheckOutDate);

        dbContext.ListingAvailabilityBlocks.Add(block);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<AvailabilityBlockDto>.Success(
            new AvailabilityBlockDto(block.Id, block.CheckInDate, block.CheckOutDate, block.BlockType));
    }
}
