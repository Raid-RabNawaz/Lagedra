using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Infrastructure.Eventing;

public sealed partial class OutboxDispatcher(
    IServiceProvider serviceProvider,
    IOptions<OutboxOptions> options,
    ILogger<OutboxDispatcher> logger)
    : BackgroundService
{
    private readonly OutboxOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarted(logger, _options.PollingIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await TickAsync(stoppingToken).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken)
                .ConfigureAwait(false);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = serviceProvider.CreateAsyncScope();

            var dbContext = scope.ServiceProvider.GetService<DbContext>();
            if (dbContext is null)
            {
                return;
            }

            var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();
            await processor.ProcessAsync(dbContext, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutdown — not an error
        }
#pragma warning disable CA1031 // intentional: dispatcher must not crash the host
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogTickFailed(logger, ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OutboxDispatcher started. Polling every {IntervalSeconds}s")]
    private static partial void LogStarted(ILogger logger, int intervalSeconds);

    [LoggerMessage(Level = LogLevel.Error, Message = "OutboxDispatcher tick failed")]
    private static partial void LogTickFailed(ILogger logger, Exception ex);
}
