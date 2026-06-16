using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Jobs;

/// <summary>
/// Refreshes availability calendars for every mapped listing on each active
/// channel connection. Runs more frequently than content sync so blocked dates
/// stay fresh and double-bookings are avoided.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class ChannelAvailabilitySyncJob(
    ChannelDbContext dbContext,
    IChannelProviderRegistry providers,
    IEncryptionService encryption,
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
            var provider = providers.Resolve(connection.ProviderKey);
            if (provider is null)
            {
                LogNoProvider(logger, connection.ProviderKey, connection.Id);
                continue;
            }

            var credentials = connection.ToCredentials(encryption);

            var maps = await dbContext.ListingMaps
                .Where(m => m.ConnectionId == connection.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            var totalBlocks = 0;
            foreach (var map in maps)
            {
                var calendar = await provider
                    .PullAvailabilityAsync(credentials, map.ProviderListingId, ct)
                    .ConfigureAwait(false);

                // TODO: project calendar.Blocks onto the mapped Lagedra listing's
                // availability so the booking flow blocks the right dates.
                totalBlocks += calendar.Blocks.Count;
            }

            LogSynced(logger, connection.Id, maps.Count, totalBlocks);
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
}
