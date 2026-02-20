using Lagedra.Infrastructure.Persistence.Interceptors;
using Lagedra.SharedKernel.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Infrastructure.Persistence;

public abstract class BaseDbContext(DbContextOptions options, IClock clock) : DbContext(options), IUnitOfWork
{
    private readonly AuditingInterceptor _auditInterceptor = new(clock);
    private readonly OutboxInterceptor _outboxInterceptor = new();
    private readonly SoftDeleteInterceptor _softDeleteInterceptor = new();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(_auditInterceptor, _outboxInterceptor, _softDeleteInterceptor);
        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfiguration(new Configurations.OutboxMessageConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
