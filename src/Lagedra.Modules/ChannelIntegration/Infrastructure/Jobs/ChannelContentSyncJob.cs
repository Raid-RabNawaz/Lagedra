using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Jobs;

/// <summary>
/// Pulls listing content from every active channel connection and (once wired)
/// upserts it into Lagedra's catalog. Provider-agnostic: each connection is
/// routed to its IChannelProvider by ProviderKey.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class ChannelContentSyncJob(
    ChannelDbContext dbContext,
    IChannelProviderRegistry providers,
    IEncryptionService encryption,
    IClock clock,
    ILogger<ChannelContentSyncJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;

        var connections = await dbContext.Connections
            .Where(c => c.Status == ChannelConnectionStatus.Active)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (connections.Count == 0)
        {
            LogNothing(logger);
            return;
        }

        foreach (var connection in connections)
        {
            var provider = providers.Resolve(connection.ProviderKey);
            if (provider is null)
            {
                LogNoProvider(logger, connection.ProviderKey, connection.Id);
                continue;
            }

            var listings = await provider
                .PullListingsAsync(connection.ToCredentials(encryption), ct)
                .ConfigureAwait(false);

            // TODO: upsert each snapshot into ListingAndLocation via a SharedKernel
            // importer, then reconcile ChannelListingMap rows (ProviderListingId -> ListingId).
            connection.RecordContentSync(clock);
            LogSynced(logger, connection.Id, connection.ProviderKey, listings.Count);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No active channel connections to content-sync")]
    private static partial void LogNothing(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No channel provider registered for key '{ProviderKey}' (connection {ConnectionId}) — skipping")]
    private static partial void LogNoProvider(ILogger logger, string providerKey, Guid connectionId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Content-synced connection {ConnectionId} ({ProviderKey}); pulled {Count} listing(s)")]
    private static partial void LogSynced(ILogger logger, Guid connectionId, string providerKey, int count);
}
