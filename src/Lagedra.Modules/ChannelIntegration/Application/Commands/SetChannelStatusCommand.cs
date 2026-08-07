using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>Enables or disables a host's channel connection (gates all syncing).</summary>
public sealed record SetChannelStatusCommand(
    Guid ConnectionId,
    Guid HostUserId,
    bool Enable) : IRequest<Result>;

public sealed class SetChannelStatusCommandHandler(
    ChannelDbContext dbContext,
    IClock clock) : IRequestHandler<SetChannelStatusCommand, Result>
{
    private static readonly Error NotFound = new(
        "Channel.NotFound",
        "Channel connection not found.");

    public async Task<Result> Handle(SetChannelStatusCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var connection = await dbContext.Connections
            .FirstOrDefaultAsync(
                c => c.Id == request.ConnectionId
                  && c.HostUserId == request.HostUserId
                  && c.Status != ChannelConnectionStatus.Revoked,
                cancellationToken)
            .ConfigureAwait(false);

        if (connection is null)
        {
            return Result.Failure(NotFound);
        }

        if (request.Enable)
        {
            connection.Activate(clock);
        }
        else
        {
            connection.Disable(clock);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
