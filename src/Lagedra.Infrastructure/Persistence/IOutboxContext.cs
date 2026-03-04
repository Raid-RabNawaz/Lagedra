using Microsoft.EntityFrameworkCore;

namespace Lagedra.Infrastructure.Persistence;

/// <summary>
/// Marks a DbContext as owning an outbox_messages table.
/// Each module that extends BaseDbContext implements this automatically.
/// The OutboxDispatcher resolves IEnumerable&lt;IOutboxContext&gt; and processes
/// each independently — no cross-module row collisions because every module's
/// outbox lives in its own schema (e.g. truth_surface.outbox_messages).
/// </summary>
public interface IOutboxContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
