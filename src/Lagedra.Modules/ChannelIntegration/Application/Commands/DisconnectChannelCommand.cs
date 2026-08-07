using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// Disconnects a host's PMS connection: the stored API credentials are destroyed,
/// all syncing and booking pushes stop, and the connection disappears from the
/// host's channel list, freeing them to connect a different account for that
/// provider.
///
/// Listings already imported are deliberately left alone — they are the host's
/// listings now, and some may be published. The provider-listing mappings are
/// kept too (attached to the retained, revoked connection) so reconnecting the
/// same account updates those drafts rather than importing a duplicate of every
/// property. Sync cursors are dropped so a reconnect starts from a clean slate.
///
/// For OAuth connections the token is also revoked at the provider, so
/// disconnecting here really does end Lagedra's access to the host's account
/// rather than just forgetting how to use it.
/// </summary>
public sealed record DisconnectChannelCommand(
    Guid ConnectionId,
    Guid HostUserId) : IRequest<Result>;

public sealed class DisconnectChannelCommandHandler(
    ChannelDbContext dbContext,
    OwnerRezOAuthClient ownerRezOAuth,
    IEncryptionService encryption,
    IClock clock) : IRequestHandler<DisconnectChannelCommand, Result>
{
    private static readonly Error NotFound = new(
        "Channel.NotFound",
        "Channel connection not found.");

    public async Task<Result> Handle(DisconnectChannelCommand request, CancellationToken cancellationToken)
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

        // Best-effort, and before Revoke() wipes the token: if OwnerRez cannot be
        // reached the host still gets disconnected here, and the token dies with
        // the row anyway.
        if (connection.ProviderKey == OwnerRezOAuthFlow.ProviderKey
            && !string.IsNullOrEmpty(connection.EncryptedSecret))
        {
            var accessToken = encryption.Decrypt(connection.EncryptedSecret);
            if (accessToken.StartsWith("at_", StringComparison.Ordinal))
            {
                await ownerRezOAuth.RevokeAsync(accessToken, cancellationToken).ConfigureAwait(false);
            }
        }

        connection.Revoke(clock);

        var cursors = await dbContext.SyncCursors
            .Where(c => c.ConnectionId == connection.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        dbContext.SyncCursors.RemoveRange(cursors);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
