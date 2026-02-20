using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Infrastructure.Eventing;

public static class EventBusExtensions
{
    /// <summary>
    /// Registers a domain event handler so the InMemoryEventBus can resolve it.
    /// </summary>
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        services.AddScoped<IDomainEventHandler<TEvent>, THandler>();
        return services;
    }
}
