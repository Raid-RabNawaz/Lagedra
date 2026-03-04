using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Infrastructure.Eventing;

public static class EventBusExtensions
{
    /// <summary>
    /// Registers a domain event handler so the InMemoryEventBus can resolve it.
    /// Call this once per handler in the module's service registration.
    /// </summary>
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        services.AddScoped<IDomainEventHandler<TEvent>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a module DbContext as an IOutboxContext so the OutboxDispatcher
    /// will poll its outbox table on every tick.
    ///
    /// Each module that has a DbContext extending BaseDbContext must call this
    /// in its own service registration. Example:
    ///   services.AddOutboxContext&lt;TruthSurfaceDbContext&gt;();
    ///
    /// The DI container resolves IEnumerable&lt;IOutboxContext&gt; and the dispatcher
    /// processes each independently — no cross-module row collisions because every
    /// module's outbox lives in its own schema.
    /// </summary>
    public static IServiceCollection AddOutboxContext<TContext>(
        this IServiceCollection services)
        where TContext : BaseDbContext
    {
        services.AddScoped<IOutboxContext>(sp => sp.GetRequiredService<TContext>());
        return services;
    }
}
