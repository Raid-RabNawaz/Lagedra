using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.Notifications.Infrastructure.Dispatch;

/// <summary>
/// Cheap in-process poller for environments that have no dedicated worker
/// (staging). Live keeps Quartz on the existing worker and must leave
/// <c>Notifications:ProcessInProcess</c> off to avoid double-sends.
/// </summary>
public sealed partial class NotificationDispatchHostedService(
    IServiceProvider serviceProvider,
    ILogger<NotificationDispatchHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger, (int)PollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await TickAsync(stoppingToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(PollInterval, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        try
        {
            var scope = serviceProvider.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                var dispatcher = scope.ServiceProvider
                    .GetRequiredService<NotificationDispatchService>();
                await dispatcher.ProcessBatchAsync(ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutdown — not an error
        }
#pragma warning disable CA1031 // the poller must not crash the API host
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogTickFailed(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "In-process notification dispatch started. Polling every {IntervalSeconds}s")]
    private static partial void LogStarted(ILogger logger, int intervalSeconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "In-process notification dispatch tick failed")]
    private static partial void LogTickFailed(ILogger logger, Exception ex);
}
