using System.Data.Common;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>
/// Cross-module implementation of <see cref="IChannelBookingPublisher"/>.
/// Pushes a confirmed, already-paid (MOR) booking back to the channel the
/// listing originated from. Idempotent per deal and fully isolated from the
/// payment flow — a failure here is logged and retried, never surfaced.
/// </summary>
public sealed partial class ChannelBookingPublisher(
    ChannelDbContext dbContext,
    IChannelProviderRegistry providers,
    ILogger<ChannelBookingPublisher> logger) : IChannelBookingPublisher
{
    public async Task PublishConfirmedBookingAsync(Guid dealId, CancellationToken ct = default)
    {
        try
        {
            var alreadyLinked = await dbContext.BookingLinks
                .AsNoTracking()
                .AnyAsync(b => b.DealId == dealId, ct)
                .ConfigureAwait(false);

            if (alreadyLinked)
            {
                LogAlreadyLinked(logger, dealId);
                return;
            }

            // TODO: resolve the deal's source listing + its ChannelConnection
            // (via ChannelListingMap), build a ChannelBookingPushRequest with
            // PaymentStatus = "paid" and NO card data, call the resolved
            // provider's PushBookingAsync, then persist a ChannelBookingLink
            // carrying the returned external booking id.
            LogPublishPending(logger, dealId, providers.All.Count);
        }
        catch (DbException ex)
        {
            // Channel delivery is best-effort: it must never roll back or block
            // payment confirmation / deal activation. A reconciliation job retries.
            LogPublishFailed(logger, dealId, ex);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Deal {DealId} is already linked to a channel booking — skipping push")]
    private static partial void LogAlreadyLinked(ILogger logger, Guid dealId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Channel booking push pending for deal {DealId} ({ProviderCount} provider(s) registered) — provider push not yet wired")]
    private static partial void LogPublishPending(ILogger logger, Guid dealId, int providerCount);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Channel booking publish failed for deal {DealId}; will retry later")]
    private static partial void LogPublishFailed(ILogger logger, Guid dealId, Exception ex);
}
