using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Jobs;

/// <summary>
/// Refreshes availability calendars for mapped listings that opted into
/// calendar sync. Runs more frequently than content sync so blocked dates
/// stay fresh and double-bookings are avoided.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class ChannelAvailabilitySyncJob(
    ChannelDbContext dbContext,
    IChannelProviderRegistry providers,
    IEncryptionService encryption,
    IListingProvider listings,
    ILogger<ChannelAvailabilitySyncJob> logger) : IJob
{
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

            // One broken connection (revoked credentials, provider outage)
            // must not abort the sync for every other connection.
            try
            {
                var credentials = connection.ToCredentials(encryption);

                var maps = await dbContext.ListingMaps
                    .Where(m => m.ConnectionId == connection.Id
                        && m.SyncCalendarFromPms
                        && m.ListingId != null)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);

                var totalBlocks = 0;
                foreach (var map in maps)
                {
                    var calendar = await provider
                        .PullAvailabilityAsync(credentials, map.ProviderListingId, ct)
                        .ConfigureAwait(false);

                    var ranges = ChannelCalendarProjection.ToHostBlockedRanges(calendar.Blocks);
                    await listings
                        .ReplaceHostBlockedDatesAsync(map.ListingId!.Value, ranges, ct)
                        .ConfigureAwait(false);
                    totalBlocks += ranges.Count;
                }

                LogSynced(logger, connection.Id, maps.Count, totalBlocks);
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
        Message = "Availability-synced connection {ConnectionId}; {ListingCount} listing(s), {BlockCount} block(s)")]
    private static partial void LogSynced(ILogger logger, Guid connectionId, int listingCount, int blockCount);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Availability sync failed for connection {ConnectionId} (provider '{ProviderKey}') — continuing with next connection")]
    private static partial void LogConnectionFailed(ILogger logger, Guid connectionId, string providerKey, Exception ex);
}
