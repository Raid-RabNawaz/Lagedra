using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Infrastructure.Eventing;

public sealed class InMemoryEventBus(IServiceProvider serviceProvider) : IEventBus
{
    public async Task Publish<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var handlers = serviceProvider.GetServices<IDomainEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            await handler.Handle(domainEvent, ct).ConfigureAwait(false);
        }
    }
}
