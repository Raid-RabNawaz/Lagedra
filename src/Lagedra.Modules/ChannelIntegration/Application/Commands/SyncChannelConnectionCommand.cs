using Lagedra.Infrastructure.External.Channels.Hostaway;
using Lagedra.Modules.ChannelIntegration.Application.DTOs;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ChannelIntegration.Application.Commands;

/// <summary>
/// On-demand content sync for one of the host's connections: pulls the latest
/// listings from the provider and imports them into Lagedra as drafts. Also
/// activates the connection if it was still pending so "Connect → Import" works
/// in a single host action. For Hostaway, auto-registers the inbound unified
/// webhook after a successful sync (when <c>App:BaseUrl</c> is publicly reachable).
/// </summary>
public sealed record SyncChannelConnectionCommand(
    Guid ConnectionId,
    Guid HostUserId) : IRequest<Result<ChannelSyncResultDto>>;

public sealed partial class SyncChannelConnectionCommandHandler(
    ChannelDbContext dbContext,
    ChannelContentImporter importer,
    HostawayChannelProvider hostaway,
    IEncryptionService encryption,
    IOptions<HostawayChannelSettings> hostawaySettings,
    IConfiguration configuration,
    IClock clock,
    ILogger<SyncChannelConnectionCommandHandler> logger)
    : IRequestHandler<SyncChannelConnectionCommand, Result<ChannelSyncResultDto>>
{
    private static readonly Error NotFound = new(
        "Channel.NotFound",
        "Channel connection not found.");

    public async Task<Result<ChannelSyncResultDto>> Handle(
        SyncChannelConnectionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var connection = await dbContext.Connections
            .FirstOrDefaultAsync(
                c => c.Id == request.ConnectionId && c.HostUserId == request.HostUserId,
                cancellationToken)
            .ConfigureAwait(false);

        if (connection is null)
        {
            return Result<ChannelSyncResultDto>.Failure(NotFound);
        }

        // A host triggering a sync implicitly opts the connection in.
        connection.Activate(clock);

        var result = await importer.SyncAsync(connection, cancellationToken).ConfigureAwait(false);

        bool? webhookRegistered = null;
        if (string.Equals(connection.ProviderKey, "hostaway", StringComparison.OrdinalIgnoreCase)
            && hostawaySettings.Value.AutoRegisterWebhooks)
        {
            webhookRegistered = await TryRegisterHostawayWebhookAsync(connection, cancellationToken)
                .ConfigureAwait(false);
        }

        return Result<ChannelSyncResultDto>.Success(
            new ChannelSyncResultDto(result.Pulled, result.Created, result.Updated, webhookRegistered));
    }

    /// <returns>
    /// <c>true</c> when the webhook is present (created or already registered),
    /// <c>false</c> when registration failed, <c>null</c> when skipped (localhost / missing base URL).
    /// </returns>
    private async Task<bool?> TryRegisterHostawayWebhookAsync(
        Domain.Aggregates.ChannelConnection connection,
        CancellationToken ct)
    {
        var apiBase = configuration["App:BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBase)
            || !Uri.TryCreate(apiBase.TrimEnd('/') + "/v1/webhooks/hostaway", UriKind.Absolute, out var callback))
        {
            LogWebhookSkippedMissingBase(logger);
            return null;
        }

        // Localhost / private hosts are rejected by Hostaway — skip quietly.
        if (callback.Host is "localhost" or "127.0.0.1" or "::1"
            || callback.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
        {
            var host = callback.Host;
            LogWebhookSkipped(logger, host);
            return null;
        }

        try
        {
            var cfg = hostawaySettings.Value;
            var outcome = await hostaway.EnsureUnifiedWebhookAsync(
                    connection.ToCredentials(encryption),
                    callback,
                    cfg.WebhookUsername,
                    cfg.WebhookPassword,
                    ct)
                .ConfigureAwait(false);

            return outcome switch
            {
                HostawayWebhookEnsureResult.Created or HostawayWebhookEnsureResult.AlreadyPresent => true,
                HostawayWebhookEnsureResult.Failed => false,
                _ => null,
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            LogWebhookFailed(logger, ex);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[Hostaway] skipped webhook auto-register — App:BaseUrl is missing or invalid")]
    private static partial void LogWebhookSkippedMissingBase(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[Hostaway] skipped webhook auto-register — callback host '{Host}' is not reachable by Hostaway")]
    private static partial void LogWebhookSkipped(ILogger logger, string host);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[Hostaway] webhook auto-register failed")]
    private static partial void LogWebhookFailed(ILogger logger, Exception ex);
}
