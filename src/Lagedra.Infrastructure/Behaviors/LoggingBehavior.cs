using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.Behaviors;

public sealed partial class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        LogHandlingStarted(logger, RequestName);

        var stopwatch = Stopwatch.StartNew();

        var response = await next().ConfigureAwait(false);

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > 500)
        {
            LogSlowHandler(logger, RequestName, elapsedMs);
        }
        else
        {
            LogHandlingCompleted(logger, RequestName, elapsedMs);
        }

        return response;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Handling {RequestName}")]
    private static partial void LogHandlingStarted(ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Handled {RequestName} in {ElapsedMs}ms")]
    private static partial void LogHandlingCompleted(ILogger logger, string requestName, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Slow handler detected: {RequestName} took {ElapsedMs}ms (threshold: 500ms)")]
    private static partial void LogSlowHandler(ILogger logger, string requestName, long elapsedMs);
}
