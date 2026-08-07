using Lagedra.Infrastructure.External.Channels.OwnerRez;
using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;
using Lagedra.Modules.ChannelIntegration.Domain.Enums;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Infrastructure.Services;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Jobs;

/// <summary>
/// Renews OwnerRez access tokens before they lapse. OwnerRez's standard policy
/// issues tokens that expire after thirty days, and an expired token means the
/// host has to reconnect by hand — so this runs daily and renews anything inside
/// the configured lead window, which tolerates several missed runs.
/// </summary>
[DisallowConcurrentExecution]
public sealed partial class OwnerRezTokenRefreshJob(
    ChannelDbContext dbContext,
    OwnerRezOAuthClient oauthClient,
    IOptions<OwnerRezChannelSettings> settings,
    IEncryptionService encryption,
    IClock clock,
    ILogger<OwnerRezTokenRefreshJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var ct = context.CancellationToken;

        // Renewal is authenticated with the OAuth app's own client credentials, so
        // without them every attempt would come back 401 and flag the connection as
        // if OwnerRez had refused. Better to leave those connections alone: they
        // keep working until their tokens lapse, and the provider then tells the
        // host to reconnect.
        if (!settings.Value.IsOAuthConfigured)
        {
            LogOAuthNotConfigured(logger);
            return;
        }

        var lead = TimeSpan.FromDays(Math.Max(1, settings.Value.TokenRefreshLeadDays));
        var cutoff = clock.UtcNow.Add(lead);

        // Disabled connections are included: a host who re-enables one a month
        // later should not find its token quietly dead.
        var due = await dbContext.Connections
            .Where(c => c.ProviderKey == OwnerRezOAuthFlow.ProviderKey
                     && c.Status != ChannelConnectionStatus.Revoked
                     && c.EncryptedRefreshToken != null
                     && c.TokenExpiresAt != null
                     && c.TokenExpiresAt <= cutoff)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (due.Count == 0)
        {
            LogNothingDue(logger);
            return;
        }

        var refreshed = 0;
        var touched = false;
        foreach (var connection in due)
        {
            try
            {
                // Either outcome edits the connection — a new token, or the error
                // explaining why there isn't one — so both need persisting.
                touched = true;
                if (await TryRefreshAsync(connection, ct).ConfigureAwait(false))
                {
                    refreshed++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogRefreshFailed(logger, connection.Id, ex);
            }
        }

        if (touched)
        {
            await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        LogSummary(logger, refreshed, due.Count);
    }

    private async Task<bool> TryRefreshAsync(ChannelConnection connection, CancellationToken ct)
    {
        var refreshToken = encryption.Decrypt(connection.EncryptedRefreshToken!);

        var tokens = await oauthClient.RefreshAsync(refreshToken, ct).ConfigureAwait(false);
        if (tokens is null)
        {
            // OwnerRez refuses a refresh once the host revokes access on their
            // side, so flag the connection instead of retrying forever silently.
            connection.MarkError(
                "OwnerRez would not renew Lagedra's access. Disconnect and connect OwnerRez again.",
                clock);
            return false;
        }

        connection.StoreOAuthTokens(
            encryption.Encrypt(tokens.AccessToken),
            string.IsNullOrWhiteSpace(tokens.RefreshToken)
                ? connection.EncryptedRefreshToken
                : encryption.Encrypt(tokens.RefreshToken),
            tokens.ExpiresAt,
            clock);

        return true;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "No OwnerRez tokens are due for refresh")]
    private static partial void LogNothingDue(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Skipping OwnerRez token refresh: no OAuth app is configured, so any connection "
                  + "still holding an OwnerRez access token will need reconnecting with an API key "
                  + "once that token expires")]
    private static partial void LogOAuthNotConfigured(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "OwnerRez token refresh failed for connection {ConnectionId}")]
    private static partial void LogRefreshFailed(ILogger logger, Guid connectionId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Refreshed {Refreshed} of {Due} OwnerRez access tokens")]
    private static partial void LogSummary(ILogger logger, int refreshed, int due);
}
