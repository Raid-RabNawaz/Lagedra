using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// Host chooses whether this imported listing's price and/or calendar stay
/// Lagedra-owned or get overwritten from the PMS on each sync job.
/// </summary>
public sealed record SetChannelListingSyncCommand(
    Guid ConnectionId,
    Guid MapId,
    Guid HostUserId,
    bool SyncPriceFromPms,
    bool SyncCalendarFromPms) : IRequest<Result<ChannelListingMapDto>>;

public sealed class SetChannelListingSyncCommandHandler(
    ChannelDbContext dbContext,
    IClock clock) : IRequestHandler<SetChannelListingSyncCommand, Result<ChannelListingMapDto>>
{
    private static readonly Error NotFound = new(
        "Channel.NotFound",
        "Channel connection not found.");

    private static readonly Error MapNotFound = new(
        "Channel.ListingMapNotFound",
        "Imported listing not found on this connection.");

    public async Task<Result<ChannelListingMapDto>> Handle(
        SetChannelListingSyncCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ownsConnection = await dbContext.Connections
            .AsNoTracking()
            .AnyAsync(
                c => c.Id == request.ConnectionId
                  && c.HostUserId == request.HostUserId
                  && c.Status != ChannelConnectionStatus.Revoked,
                cancellationToken)
            .ConfigureAwait(false);

        if (!ownsConnection)
        {
            return Result<ChannelListingMapDto>.Failure(NotFound);
        }

        var map = await dbContext.ListingMaps
            .FirstOrDefaultAsync(
                m => m.Id == request.MapId && m.ConnectionId == request.ConnectionId,
                cancellationToken)
            .ConfigureAwait(false);

        if (map is null)
        {
            return Result<ChannelListingMapDto>.Failure(MapNotFound);
        }

        map.SetSyncPreferences(request.SyncPriceFromPms, request.SyncCalendarFromPms, clock);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<ChannelListingMapDto>.Success(ChannelListingMapMapper.ToDto(map));
    }
}
