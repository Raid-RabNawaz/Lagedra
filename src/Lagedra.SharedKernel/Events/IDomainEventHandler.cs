using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Events;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken ct = default);
}
