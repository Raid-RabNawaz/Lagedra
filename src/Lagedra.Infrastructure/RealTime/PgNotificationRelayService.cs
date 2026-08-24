using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Lagedra.Infrastructure.RealTime;

/// <summary>
/// Runs in the hub-owning host (the API). Listens on the shared Postgres
/// NOTIFY channel and forwards pushes published by other hosts (the worker's
/// <see cref="PgNotifyNotificationPusher"/>) to connected browsers via the
/// SignalR hub. Keeps a dedicated connection open and reconnects with a
/// short delay after any failure.
/// </summary>
public sealed partial class PgNotificationRelayService(
    IConfiguration configuration,
    IHubContext<NotificationHub> hubContext,
    ILogger<PgNotificationRelayService> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(stoppingToken).ConfigureAwait(false);

                connection.Notification += (_, args) => _ = RelayAsync(args.Payload, stoppingToken);

                await using (var listen = new NpgsqlCommand($"LISTEN {InAppPushChannel.Name}", connection))
                {
                    await listen.ExecuteNonQueryAsync(stoppingToken).ConfigureAwait(false);
                }

                LogListening(logger, InAppPushChannel.Name);

                while (!stoppingToken.IsCancellationRequested)
                {
                    await connection.WaitAsync(stoppingToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Shutdown — not an error.
            }
#pragma warning disable CA1031 // the relay must outlive any transient DB failure
            catch (Exception ex)
#pragma warning restore CA1031
            {
                LogListenLoopFailed(logger, ex);

                try
                {
                    await Task.Delay(ReconnectDelay, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Shutdown while backing off.
                }
            }
        }
    }

    private async Task RelayAsync(string payload, CancellationToken ct)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<InAppPushEnvelope>(payload);
            if (envelope is null || envelope.UserIds.Count == 0)
            {
                return;
            }

            var groups = envelope.UserIds.Select(id => $"user:{id}").ToList();
            await hubContext.Clients
                .Groups(groups)
                .SendAsync("ReceiveNotification", envelope.Notification, ct)
                .ConfigureAwait(false);
        }
#pragma warning disable CA1031 // a malformed payload must not kill the listener
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogRelayFailed(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Relaying real-time pushes from Postgres channel '{Channel}' to the SignalR hub")]
    private static partial void LogListening(ILogger logger, string channel);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Real-time push relay listen loop failed; reconnecting shortly")]
    private static partial void LogListenLoopFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to relay a real-time push to the SignalR hub")]
    private static partial void LogRelayFailed(ILogger logger, Exception ex);
}
