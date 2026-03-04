using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.Behaviors;

public sealed partial class UnhandledExceptionBehavior<TRequest, TResponse>(
    ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogUnhandledException(logger, RequestName, ex);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Unhandled exception in handler for {RequestName}")]
    private static partial void LogUnhandledException(ILogger logger, string requestName, Exception exception);
}
