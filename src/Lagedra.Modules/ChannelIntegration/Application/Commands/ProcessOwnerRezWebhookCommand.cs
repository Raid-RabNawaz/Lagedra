using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
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
/// Handles OwnerRez webhook deliveries for the Lagedra OAuth app. The payload
/// carries the changed record inline, so a booking change needs no follow-up API
/// call — see https://www.ownerrez.com/support/articles/api-webhooks.
///
/// Anything we do not act on is still acknowledged: OwnerRez retries non-2xx
/// deliveries ten times and auto-disables apps that fail often, so staying quiet
/// about an unfamiliar event is much cheaper than erroring on it.
///
/// Deliveries can legitimately arrive twice (OwnerRez re-sends when it does not see
/// a timely 2xx), and there is no de-duplication store because each action is
/// idempotent: a cancellation is applied only while the link is not already
/// cancelled, and revoking an already-revoked connection is a no-op.
/// </summary>
public sealed record ProcessOwnerRezWebhookCommand(
    string? AuthorizationHeader,
    string Payload) : IRequest<Result>;

public sealed partial class ProcessOwnerRezWebhookCommandHandler(
    ChannelDbContext dbContext,
    ChannelBookingUpdateReconciler reconciler,
    IOptions<OwnerRezChannelSettings> settings,
    IClock clock,
    ILogger<ProcessOwnerRezWebhookCommandHandler> logger)
    : IRequestHandler<ProcessOwnerRezWebhookCommand, Result>
{
    private static readonly Error Unauthorized = new(
        "Channel.WebhookUnauthorized",
        "Invalid OwnerRez webhook credentials.");

    private static readonly Error InvalidPayload = new(
        "Channel.WebhookInvalidPayload",
        "OwnerRez webhook payload could not be parsed.");

    public async Task<Result> Handle(
        ProcessOwnerRezWebhookCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cfg = settings.Value;
        if (!OwnerRezWebhookParser.IsAuthorized(request.AuthorizationHeader, cfg))
        {
            if (!cfg.IsWebhookAuthConfigured)
            {
                LogAuthNotConfigured(logger);
            }

            return Result.Failure(Unauthorized);
        }

        if (OwnerRezWebhookParser.TryParse(request.Payload) is not { } delivery)
        {
            return Result.Failure(InvalidPayload);
        }

        // The test ping from OwnerRez's "Send a Test Webhook" button carries no
        // account context and exists only to prove the URL and credentials work.
        if (delivery.IsTestPing)
        {
            LogTestPing(logger);
            return Result.Success();
        }

        var connection = await FindConnectionAsync(delivery.UserId, cancellationToken)
            .ConfigureAwait(false);

        if (connection is null)
        {
            // A host who disconnected in Lagedra but left the app authorized in
            // OwnerRez keeps sending these; there is nothing left to update.
            LogUnknownAccount(logger, delivery.UserId ?? "(missing)", delivery.Action);
            return Result.Success();
        }

        if (delivery.IsAuthorizationRevoked)
        {
            // The host removed Lagedra from their OwnerRez account, so the stored
            // token is already dead. Revoking here stops every future sync and
            // booking push instead of letting them fail one at a time.
            connection.Revoke(clock);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            LogRevoked(logger, connection.Id);
            return Result.Success();
        }

        if (delivery.BookingUpdate is { } update)
        {
            var applied = await reconciler
                .ApplyAsync(connection.Id, OwnerRezOAuthFlow.ProviderKey, [update], cancellationToken)
                .ConfigureAwait(false);

            connection.RecordBookingSync(clock);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            LogBookingProcessed(logger, delivery.Action, update.ExternalBookingId, applied);
            return Result.Success();
        }

        // Property, guest and the rest are acknowledged only. Listing content is
        // refreshed by the scheduled content sync rather than inline, because
        // OwnerRez allows two seconds to respond and re-importing an account's
        // properties does not fit in that budget.
        LogAcknowledged(logger, delivery.Action, delivery.EntityType);
        return Result.Success();
    }

    private async Task<ChannelConnection?> FindConnectionAsync(string? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        // OAuth connections store the OwnerRez user id as the external account id,
        // which is how the webhook envelope identifies the account.
        return await dbContext.Connections
            .FirstOrDefaultAsync(
                c => c.ProviderKey == OwnerRezOAuthFlow.ProviderKey
                  && c.ExternalAccountId == userId
                  && c.Status != ChannelConnectionStatus.Revoked,
                ct)
            .ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[OwnerRez webhook] test ping acknowledged")]
    private static partial void LogTestPing(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error,
        Message = "[OwnerRez webhook] rejected a delivery because Channels:OwnerRez:WebhookUsername/"
                  + "WebhookPassword are not configured")]
    private static partial void LogAuthNotConfigured(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "[OwnerRez webhook] no live connection for account {UserId} — ignoring {Action}")]
    private static partial void LogUnknownAccount(ILogger logger, string userId, string action);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[OwnerRez webhook] host revoked access; connection {ConnectionId} disconnected")]
    private static partial void LogRevoked(ILogger logger, Guid connectionId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "[OwnerRez webhook] acknowledged {Action} for {EntityType} without acting on it")]
    private static partial void LogAcknowledged(ILogger logger, string action, string entityType);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[OwnerRez webhook] processed {Action} for booking {BookingId} (applied {Applied})")]
    private static partial void LogBookingProcessed(
        ILogger logger, string action, string bookingId, int applied);
}
