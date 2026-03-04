using System.Text.Json;
using Lagedra.SharedKernel.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lagedra.Infrastructure.Persistence.Interceptors;

public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (eventData.Context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var aggregates = eventData.Context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => ((AggregateRoot<Guid>)(object)a).DomainEvents.Count != 0)
            .ToList();

        foreach (var aggregate in aggregates)
        {
            var root = (AggregateRoot<Guid>)(object)aggregate;

            var outboxMessages = root.DomainEvents
                .Select(e => new OutboxMessage
                {
                    Type = e.GetType().AssemblyQualifiedName!,
                    Content = JsonSerializer.Serialize(e, e.GetType()),
                    OccurredAt = e.OccurredAt
                })
                .ToList();

            eventData.Context.Set<OutboxMessage>().AddRange(outboxMessages);
            root.ClearDomainEvents();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
