using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// On-demand content sync for one of the host's connections: pulls the latest
/// listings from the provider and imports them into Lagedra as drafts. Also
/// activates the connection if it was still pending so "Connect → Import" works
/// in a single host action.
/// </summary>
public sealed record SyncChannelConnectionCommand(
    Guid ConnectionId,
    Guid HostUserId) : IRequest<Result<ChannelSyncResultDto>>;

public sealed class SyncChannelConnectionCommandHandler(
    ChannelDbContext dbContext,
    ChannelContentImporter importer,
    IClock clock) : IRequestHandler<SyncChannelConnectionCommand, Result<ChannelSyncResultDto>>
{
    private static readonly Error NotFound = new(
        "Channel.NotFound",
        "Channel connection not found.");

    public async Task<Result<ChannelSyncResultDto>> Handle(
        SyncChannelConnectionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var connection = await dbContext.Connections
            .FirstOrDefaultAsync(
                c => c.Id == request.ConnectionId && c.HostUserId == request.HostUserId,
                cancellationToken)
            .ConfigureAwait(false);

        if (connection is null)
        {
            return Result<ChannelSyncResultDto>.Failure(NotFound);
        }

        // A host triggering a sync implicitly opts the connection in.
        connection.Activate(clock);

        var result = await importer.SyncAsync(connection, cancellationToken).ConfigureAwait(false);

        return Result<ChannelSyncResultDto>.Success(
            new ChannelSyncResultDto(result.Pulled, result.Created, result.Updated));
    }
}
