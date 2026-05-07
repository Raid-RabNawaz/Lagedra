using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lagedra.Infrastructure.Eventing;

public sealed partial class InMemoryEventBus(
    IServiceProvider serviceProvider,
    ILogger<InMemoryEventBus> logger) : IEventBus
{
    public async Task Publish<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var handlers = serviceProvider.GetServices<IDomainEventHandler<TEvent>>();
        List<Exception>? failures = null;

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Handle(domainEvent, ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031
            catch (Exception ex)
#pragma warning restore CA1031
            {
                failures ??= [];
                failures.Add(ex);
                LogHandlerFailed(logger, handler.GetType().Name, typeof(TEvent).Name, ex);
            }
        }

        if (failures is { Count: > 0 })
        {
            throw new AggregateException(
                $"{failures.Count} handler(s) failed for {typeof(TEvent).Name}", failures);
        }
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Handler {HandlerName} failed for event {EventName} — continuing with remaining handlers")]
    private static partial void LogHandlerFailed(ILogger logger, string handlerName, string eventName, Exception ex);
}
