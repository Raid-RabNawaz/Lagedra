using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Jobs;

/// <summary>
/// Pulls listing content from every active channel connection and upserts it
/// into Lagedra's catalog as draft listings. Provider-agnostic: each connection
/// is routed to its IChannelProvider by ProviderKey, then materialised through
/// <see cref="ChannelContentImporter"/>.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class ChannelContentSyncJob(
    ChannelDbContext dbContext,
    ChannelContentImporter importer,
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
            try
            {
                await importer.SyncAsync(connection, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One bad connection must never abort the whole batch.
                LogConnectionFailed(logger, connection.Id, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No active channel connections to content-sync")]
    private static partial void LogNothing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Content sync failed for connection {ConnectionId}")]
    private static partial void LogConnectionFailed(ILogger logger, Guid connectionId, Exception ex);
}
