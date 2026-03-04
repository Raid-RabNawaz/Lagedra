using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Events;

public interface IEventBus
{
    Task Publish<TEvent>(TEvent domainEvent, CancellationToken ct = default)
        where TEvent : IDomainEvent;
}
