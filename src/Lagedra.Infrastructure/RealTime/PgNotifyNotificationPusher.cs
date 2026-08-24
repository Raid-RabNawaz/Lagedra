using System.Text.Json;
using Lagedra.SharedKernel.RealTime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Lagedra.Infrastructure.RealTime;

/// <summary>
/// Publishes in-app notification pushes over Postgres NOTIFY instead of the
/// local SignalR hub. Registered in hosts without browser connections (the
/// worker): the API's <see cref="PgNotificationRelayService"/> listens on the
/// same channel and forwards to its hub, so worker-delivered notifications
/// reach the browser in real time without a page refresh.
/// </summary>
public sealed partial class PgNotifyNotificationPusher(
    IConfiguration configuration,
    ILogger<PgNotifyNotificationPusher> logger) : INotificationPusher
{
    private readonly string _connectionString =
        configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

    public Task PushToUserAsync(Guid userId, InAppNotificationDto notification, CancellationToken ct = default) =>
        PushToUsersAsync([userId], notification, ct);

    public async Task PushToUsersAsync(
        IEnumerable<Guid> userIds,
        InAppNotificationDto notification,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentNullException.ThrowIfNull(notification);

        // Best-effort by design: a lost push must never fail the delivery
        // pipeline — clients catch up through their polling fallback.
        try
        {
            var envelope = new InAppPushEnvelope([.. userIds], notification);
            var payload = JsonSerializer.Serialize(envelope);

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            await using var command = new NpgsqlCommand("SELECT pg_notify(@channel, @payload)", connection);
            command.Parameters.AddWithValue("channel", InAppPushChannel.Name);
            command.Parameters.AddWithValue("payload", payload);
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // push is best-effort; polling is the fallback
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogPushFailed(logger, notification.Id, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to publish real-time push for notification {NotificationId} over Postgres NOTIFY")]
    private static partial void LogPushFailed(ILogger logger, Guid notificationId, Exception ex);
}
