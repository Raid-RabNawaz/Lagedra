using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lagedra.Infrastructure.External.Channels;
using Lagedra.Infrastructure.External.Channels.Hostaway;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// Handles Hostaway unified webhook deliveries
/// (<c>reservation.created</c> / <c>reservation.updated</c>). Unknown events
/// are acknowledged so Hostaway does not retry forever.
/// </summary>
public sealed record ProcessHostawayWebhookCommand(
    string? AuthorizationHeader,
    string Payload) : IRequest<Result>;

public sealed partial class ProcessHostawayWebhookCommandHandler(
    ChannelDbContext dbContext,
    ChannelBookingUpdateReconciler reconciler,
    IOptions<HostawayChannelSettings> settings,
    IClock clock,
    ILogger<ProcessHostawayWebhookCommandHandler> logger)
    : IRequestHandler<ProcessHostawayWebhookCommand, Result>
{
    private static readonly Error Unauthorized = new(
        "Channel.WebhookUnauthorized",
        "Invalid Hostaway webhook credentials.");

    private static readonly Error InvalidPayload = new(
        "Channel.WebhookInvalidPayload",
        "Hostaway webhook payload could not be parsed.");

    public async Task<Result> Handle(
        ProcessHostawayWebhookCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!ValidateBasicAuth(request.AuthorizationHeader, settings.Value))
        {
            return Result.Failure(Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(request.Payload))
        {
            return Result.Success();
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(request.Payload);
        }
        catch (JsonException)
        {
            return Result.Failure(InvalidPayload);
        }

        using (doc)
        {
            var root = doc.RootElement;

            // Hostaway (and Pipedream fixtures) send { "data": "test" } as a ping.
            if (root.TryGetProperty("data", out var dataEl)
                && dataEl.ValueKind == JsonValueKind.String
                && string.Equals(dataEl.GetString(), "test", StringComparison.OrdinalIgnoreCase))
            {
                LogTestPing(logger);
                return Result.Success();
            }

            var eventType = root.TryGetProperty("event", out var eventEl)
                ? eventEl.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(eventType))
            {
                // Acknowledge unfamiliar envelopes so Hostaway stops retrying.
                LogIgnoredEvent(logger, "(missing)");
                return Result.Success();
            }

            if (!eventType.StartsWith("reservation.", StringComparison.OrdinalIgnoreCase))
            {
                LogIgnoredEvent(logger, eventType);
                return Result.Success();
            }

            if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            {
                LogIgnoredEvent(logger, eventType);
                return Result.Success();
            }

            var bookingId = data.TryGetProperty("id", out var idEl) ? idEl.ToString() : null;
            if (string.IsNullOrWhiteSpace(bookingId))
            {
                LogIgnoredEvent(logger, eventType);
                return Result.Success();
            }

            var statusRaw = data.TryGetProperty("status", out var statusEl)
                ? statusEl.GetString()
                : null;
            var changedAt = ParseUtc(
                    data.TryGetProperty("latestActivityOn", out var act) ? act.GetString() : null)
                ?? ParseUtc(data.TryGetProperty("updatedOn", out var upd) ? upd.GetString() : null)
                ?? clock.UtcNow;

            var update = new ChannelBookingUpdate(
                bookingId,
                NormalizeBookingStatus(statusRaw),
                changedAt);

            var applied = await reconciler
                .ApplyAsync(connectionId: null, providerKey: "hostaway", [update], cancellationToken)
                .ConfigureAwait(false);

            if (applied > 0)
            {
                var link = await dbContext.BookingLinks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.ProviderBookingId == bookingId, cancellationToken)
                    .ConfigureAwait(false);
                if (link is not null)
                {
                    var connection = await dbContext.Connections
                        .FirstOrDefaultAsync(c => c.Id == link.ConnectionId, cancellationToken)
                        .ConfigureAwait(false);
                    connection?.RecordBookingSync(clock);
                }

                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            LogProcessed(logger, eventType, bookingId, applied);
            return Result.Success();
        }
    }

    private static bool ValidateBasicAuth(string? authorizationHeader, HostawayChannelSettings cfg)
    {
        // If no webhook credentials are configured, accept deliveries (hosts may
        // register an open URL from the Hostaway dashboard). Prefer setting them.
        if (string.IsNullOrWhiteSpace(cfg.WebhookUsername)
            && string.IsNullOrWhiteSpace(cfg.WebhookPassword))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            var encoded = authorizationHeader["Basic ".Length..].Trim();
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
            var sep = decoded.IndexOf(':', StringComparison.Ordinal);
            if (sep < 0)
            {
                return false;
            }

            var user = decoded[..sep];
            var pass = decoded[(sep + 1)..];
            return FixedEquals(user, cfg.WebhookUsername ?? string.Empty)
                   && FixedEquals(pass, cfg.WebhookPassword ?? string.Empty);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool FixedEquals(string left, string right)
    {
        var a = Encoding.UTF8.GetBytes(left);
        var b = Encoding.UTF8.GetBytes(right);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static DateTime? ParseUtc(string? raw)
        => DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AdjustToUniversal
            | System.Globalization.DateTimeStyles.AssumeUniversal, out var dt)
            ? dt
            : null;

    private static string NormalizeBookingStatus(string? status) => (status ?? string.Empty).ToUpperInvariant() switch
    {
        "NEW" or "MODIFIED" or "OWNERSTAY" => "confirmed",
        "CANCELLED" or "DECLINED" or "EXPIRED" or "INQUIRYDENIED" or "INQUIRYNOTPOSSIBLE" or "INQUIRYTIMEDOUT" =>
            "cancelled",
        _ => "pending",
    };

    [LoggerMessage(Level = LogLevel.Information, Message = "[Hostaway webhook] test ping acknowledged")]
    private static partial void LogTestPing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "[Hostaway webhook] ignored event {EventType}")]
    private static partial void LogIgnoredEvent(ILogger logger, string eventType);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[Hostaway webhook] processed {EventType} for booking {BookingId} (applied {Applied})")]
    private static partial void LogProcessed(ILogger logger, string eventType, string bookingId, int applied);
}
