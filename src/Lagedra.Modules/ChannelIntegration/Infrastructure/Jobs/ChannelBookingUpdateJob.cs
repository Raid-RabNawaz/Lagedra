using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Entities;
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
/// Pulls booking status changes from each active channel (e.g. host-side
/// cancellations) since the last high-water mark and reconciles them against
/// our <see cref="ChannelBookingLink"/> rows. Hostaway connections also receive
/// near-real-time updates via the inbound unified webhook.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class ChannelBookingUpdateJob(
    ChannelDbContext dbContext,
    IChannelProviderRegistry providers,
    IEncryptionService encryption,
    ChannelBookingUpdateReconciler reconciler,
    IClock clock,
    ILogger<ChannelBookingUpdateJob> logger) : IJob
{
    private const string CursorKind = "booking-updates";

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;

        var connections = await dbContext.Connections
            .Where(c => c.Status == ChannelConnectionStatus.Active)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var connection in connections)
        {
            if (ct.IsCancellationRequested)
            {
                break;
            }

            var provider = providers.Resolve(connection.ProviderKey);
            if (provider is null)
            {
                LogNoProvider(logger, connection.ProviderKey, connection.Id);
                continue;
            }

            // Isolate connections: one failing provider must not abort the
            // sweep, and saving per connection keeps cursor progress for the
            // connections that already succeeded this run.
            try
            {
                var cursor = await dbContext.SyncCursors
                    .FirstOrDefaultAsync(
                        c => c.ConnectionId == connection.Id && c.CursorKind == CursorKind, ct)
                    .ConfigureAwait(false);

                var since = cursor?.LastChangedAtUtc ?? clock.UtcNow.AddDays(-7);

                var updates = await provider
                    .PullBookingUpdatesAsync(connection.ToCredentials(encryption), since, ct)
                    .ConfigureAwait(false);

                var highWater = since;
                foreach (var update in updates)
                {
                    if (update.ChangedAtUtc > highWater)
                    {
                        highWater = update.ChangedAtUtc;
                    }
                }

                await reconciler
                    .ApplyAsync(connection.Id, connection.ProviderKey, updates, ct)
                    .ConfigureAwait(false);

                if (cursor is null)
                {
                    cursor = ChannelSyncCursor.Create(connection.Id, CursorKind, highWater, clock);
                    dbContext.SyncCursors.Add(cursor);
                }
                else
                {
                    cursor.Advance(highWater, clock);
                }

                connection.RecordBookingSync(clock);
                await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
                LogProcessed(logger, connection.Id, updates.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
#pragma warning disable CA1031 // isolate connections from each other
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogConnectionFailed(logger, connection.Id, connection.ProviderKey, ex);
            }
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No channel provider registered for key '{ProviderKey}' (connection {ConnectionId}) — skipping")]
    private static partial void LogNoProvider(ILogger logger, string providerKey, Guid connectionId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Processed {Count} booking update(s) for connection {ConnectionId}")]
    private static partial void LogProcessed(ILogger logger, Guid connectionId, int count);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Booking update sync failed for connection {ConnectionId} (provider '{ProviderKey}') — continuing with next connection")]
    private static partial void LogConnectionFailed(ILogger logger, Guid connectionId, string providerKey, Exception ex);
}
