using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>
/// Applies channel booking status updates (from poll jobs or inbound webhooks)
/// onto matching <c>ChannelBookingLink</c> rows.
/// </summary>
public sealed partial class ChannelBookingUpdateReconciler(
    ChannelDbContext dbContext,
    IClock clock,
    ILogger<ChannelBookingUpdateReconciler> logger)
{
    public async Task<int> ApplyAsync(
        Guid? connectionId,
        string? providerKey,
        IEnumerable<ChannelBookingUpdate> updates,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        var applied = 0;
        foreach (var update in updates)
        {
            if (string.IsNullOrWhiteSpace(update.ExternalBookingId))
            {
                continue;
            }

            var query = dbContext.BookingLinks
                .Where(b => b.ProviderBookingId == update.ExternalBookingId);

            if (connectionId.HasValue)
            {
                query = query.Where(b => b.ConnectionId == connectionId.Value);
            }
            else if (!string.IsNullOrWhiteSpace(providerKey))
            {
                query = query.Where(b =>
                    dbContext.Connections.Any(c =>
                        c.Id == b.ConnectionId
                        && c.ProviderKey == providerKey
                        && c.Status == ChannelConnectionStatus.Active));
            }

            var link = await query.FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (link is null)
            {
                LogUnknownBooking(logger, update.ExternalBookingId, providerKey ?? "any");
                continue;
            }

            if (string.Equals(update.Status, "cancelled", StringComparison.OrdinalIgnoreCase)
                && link.SyncStatus != ChannelBookingSyncStatus.CancelledRemotely)
            {
                link.MarkCancelledRemotely(clock);
                applied++;
                // TODO: raise a cross-module cancellation so ActivationAndBilling
                // can void/refund the deal as appropriate.
            }
        }

        return applied;
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No channel booking link for external booking {ExternalBookingId} (provider {ProviderKey}) — ignoring")]
    private static partial void LogUnknownBooking(ILogger logger, string externalBookingId, string providerKey);
}
