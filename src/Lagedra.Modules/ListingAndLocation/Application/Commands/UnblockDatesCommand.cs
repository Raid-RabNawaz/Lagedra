using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Application.Commands;

public sealed record UnblockDatesCommand(
    Guid ListingId,
    Guid BlockId) : IRequest<Result>;

public sealed class UnblockDatesCommandHandler(ListingsDbContext dbContext)
    : IRequestHandler<UnblockDatesCommand, Result>
{
    private static readonly Error NotFound = new("Block.NotFound", "Availability block not found.");
    private static readonly Error CannotRemoveBooked = new("Block.CannotRemove", "Cannot remove a block created by an active deal.");

    public async Task<Result> Handle(
        UnblockDatesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var block = await dbContext.ListingAvailabilityBlocks
            .FirstOrDefaultAsync(b => b.Id == request.BlockId && b.ListingId == request.ListingId, cancellationToken)
            .ConfigureAwait(false);

        if (block is null)
        {
            return Result.Failure(NotFound);
        }

        if (block.BlockType == AvailabilityBlockType.Booked)
        {
            return Result.Failure(CannotRemoveBooked);
        }

        dbContext.ListingAvailabilityBlocks.Remove(block);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
