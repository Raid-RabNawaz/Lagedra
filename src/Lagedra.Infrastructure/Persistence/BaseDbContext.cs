using System.Linq.Expressions;
using System.Reflection;
using Lagedra.Infrastructure.Persistence.Interceptors;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Infrastructure.Persistence;

/// <summary>
/// Abstract base for all module DbContexts.
///
/// Every subclass must declare its own ModuleSchema (e.g. "truth_surface").
/// The outbox_messages table is created inside that schema, so each module
/// has a completely isolated outbox — no cross-module row collisions.
///
/// Subclasses automatically implement IOutboxContext, which the OutboxDispatcher
/// uses to process each module's pending messages independently.
/// </summary>
public abstract class BaseDbContext(DbContextOptions options, IClock clock)
    : DbContext(options), IUnitOfWork, IOutboxContext
{
    private readonly AuditingInterceptor _auditInterceptor = new(clock);
    private readonly OutboxInterceptor _outboxInterceptor = new();
    private readonly SoftDeleteInterceptor _softDeleteInterceptor = new(clock);

    /// <summary>
    /// The PostgreSQL schema that owns this module's tables and outbox.
    /// Example: "truth_surface", "compliance", "activation_billing".
    /// </summary>
    protected abstract string ModuleSchema { get; }

    // IOutboxContext implementation — exposes the module-scoped outbox table
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(_auditInterceptor, _outboxInterceptor, _softDeleteInterceptor);
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // All entity tables in this module default to the module's own schema
        modelBuilder.HasDefaultSchema(ModuleSchema);

        // Outbox table lives in the same schema — e.g. truth_surface.outbox_messages
        modelBuilder.ApplyConfiguration(new Configurations.OutboxMessageConfiguration(ModuleSchema));

        base.OnModelCreating(modelBuilder);

        ApplySoftDeleteQueryFilters(modelBuilder);
    }

    private static void ApplySoftDeleteQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var property = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
            var filter = Expression.Lambda(Expression.Not(property), parameter);

            entityType.SetQueryFilter(filter);
        }
    }
}
