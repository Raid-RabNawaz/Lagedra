using System.Data.Common;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Modules.ChannelIntegration.Domain.Entities;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Services;

/// <summary>
/// Cross-module implementation of <see cref="IChannelBookingPublisher"/>.
/// Pushes a confirmed, already-paid (Merchant-of-Record) booking back to the
/// channel the listing originated from. It resolves dealId → source listing →
/// <see cref="ChannelListingMap"/> → active <c>ChannelConnection</c>, builds a
/// MoR push request (no card data, PaymentStatus = "paid") and persists a
/// <see cref="ChannelBookingLink"/> for idempotency. Fully isolated from the
/// payment flow — any failure is recorded and retried, never surfaced.
/// </summary>
public sealed partial class ChannelBookingPublisher(
    ChannelDbContext dbContext,
    IChannelProviderRegistry providers,
    IDealApplicationStatusProvider dealProvider,
    IUserEmailResolver emailResolver,
    IEncryptionService encryption,
    IClock clock,
    ILogger<ChannelBookingPublisher> logger) : IChannelBookingPublisher
{
    public async Task PublishConfirmedBookingAsync(Guid dealId, CancellationToken ct = default)
    {
        try
        {
            var link = await dbContext.BookingLinks
                .FirstOrDefaultAsync(b => b.DealId == dealId, ct)
                .ConfigureAwait(false);

            if (link is { SyncStatus: ChannelBookingSyncStatus.Pushed })
            {
                LogAlreadyLinked(logger, dealId);
                return;
            }

            var details = await dealProvider.GetDealDetailsAsync(dealId, ct).ConfigureAwait(false);
            if (details is null)
            {
                LogNoDeal(logger, dealId);
                return;
            }

            var match = await (
                from map in dbContext.ListingMaps
                join connection in dbContext.Connections on map.ConnectionId equals connection.Id
                where map.ListingId == details.ListingId
                    && map.ListingId != null
                    && connection.Status == ChannelConnectionStatus.Active
                select new { Map = map, Connection = connection })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (match is null)
            {
                // Native Lagedra listing (not channel-sourced) — nothing to push.
                LogNotChannelSourced(logger, dealId, details.ListingId);
                return;
            }

            var provider = providers.Resolve(match.Connection.ProviderKey);
            if (provider is null)
            {
                LogNoProvider(logger, match.Connection.ProviderKey, dealId);
                return;
            }

            var request = await BuildPushRequestAsync(match.Map.ProviderListingId, details, ct).ConfigureAwait(false);

            link ??= ChannelBookingLink.CreatePending(match.Connection.Id, dealId, clock);
            if (dbContext.Entry(link).State == EntityState.Detached)
            {
                dbContext.BookingLinks.Add(link);
            }

            var result = await provider
                .PushBookingAsync(match.Connection.ToCredentials(encryption), request, ct)
                .ConfigureAwait(false);

            if (result.Success && !string.IsNullOrWhiteSpace(result.ExternalBookingId))
            {
                link.MarkPushed(result.ExternalBookingId!, clock);
                LogPushed(logger, dealId, result.ExternalBookingId!);
            }
            else
            {
                var error = result.ErrorMessage ?? result.ErrorCode ?? "OwnerRez rejected the booking.";
                link.MarkFailed(error, clock);
                LogPushRejected(logger, dealId, error);
            }

            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is DbException or DbUpdateException or InvalidOperationException)
        {
            // Channel delivery is best-effort: it must never roll back or block
            // payment confirmation / deal activation.
            LogPublishFailed(logger, dealId, ex);
        }
    }

    private async Task<ChannelBookingPushRequest> BuildPushRequestAsync(
        string providerListingId,
        DealApplicationDetailsDto details,
        CancellationToken ct)
    {
        var email = await emailResolver.GetEmailAsync(details.TenantUserId, ct).ConfigureAwait(false);
        var (firstName, lastName) = SplitGuestName(email);
        var guest = new ChannelGuest(firstName, lastName, email ?? "guest@lagedra.com");

        var orderItems = new List<ChannelOrderItem>();
        if (details.FirstMonthRentCents is long rent && rent > 0)
        {
            orderItems.Add(new ChannelOrderItem("RENTAL", "First month rent", rent));
        }

        if (details.DepositAmountCents is long deposit && deposit > 0)
        {
            orderItems.Add(new ChannelOrderItem("MISC", "Security deposit", deposit));
        }

        if (details.InsuranceFeeCents is long insurance && insurance > 0)
        {
            orderItems.Add(new ChannelOrderItem("MISC", "Protection plan", insurance));
        }

        return new ChannelBookingPushRequest(
            ExternalListingId: providerListingId,
            Guest: guest,
            CheckIn: details.RequestedCheckIn,
            CheckOut: details.RequestedCheckOut,
            Adults: Math.Max(1, details.GuestCount),
            Children: 0,
            Pets: 0,
            Currency: "USD",
            OrderItems: orderItems,
            PaymentStatus: "paid",
            TrackingReference: details.DealId.ToString(),
            OwnerCommissionCents: null,
            GuestServiceFeeCents: details.ServiceFeeCents,
            Message: details.Message);
    }

    private static (string FirstName, string LastName) SplitGuestName(string? email)
    {
        // Tenant first/last name is not exposed cross-module; derive a readable
        // placeholder from the email local part for the channel guest record.
        var local = email?.Split('@', 2).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(local))
        {
            return ("Lagedra", "Guest");
        }

        var parts = local.Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries);
        var first = Capitalize(parts.Length > 0 ? parts[0] : local);
        var last = parts.Length > 1 ? Capitalize(parts[^1]) : "Guest";
        return (first, last);
    }

    private static string Capitalize(string value)
        => value.Length <= 1 ? value.ToUpperInvariant() : char.ToUpperInvariant(value[0]) + value[1..];

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Deal {DealId} is already pushed to a channel booking — skipping")]
    private static partial void LogAlreadyLinked(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Deal {DealId} not found via deal provider — cannot push to channel")]
    private static partial void LogNoDeal(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Deal {DealId} listing {ListingId} is not channel-sourced — no push needed")]
    private static partial void LogNotChannelSourced(ILogger logger, Guid dealId, Guid listingId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "No channel provider for key '{ProviderKey}' (deal {DealId}) — skipping push")]
    private static partial void LogNoProvider(ILogger logger, string providerKey, Guid dealId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Pushed deal {DealId} to channel as booking {ExternalBookingId}")]
    private static partial void LogPushed(ILogger logger, Guid dealId, string externalBookingId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Channel rejected booking push for deal {DealId}: {Error}")]
    private static partial void LogPushRejected(ILogger logger, Guid dealId, string error);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Channel booking publish failed for deal {DealId}; will retry later")]
    private static partial void LogPublishFailed(ILogger logger, Guid dealId, Exception ex);
}
