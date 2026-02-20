using Lagedra.Infrastructure.Persistence.Interceptors;
using Lagedra.SharedKernel.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Infrastructure.Persistence;

public abstract class BaseDbContext(DbContextOptions options, IClock clock) : DbContext(options), IUnitOfWork
{
    private readonly AuditingInterceptor _auditInterceptor = new(clock);
    private readonly OutboxInterceptor _outboxInterceptor = new();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        optionsBuilder.AddInterceptors(_auditInterceptor, _outboxInterceptor);
        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("outbox_messages", "outbox");
            b.HasKey(m => m.Id);
            b.Property(m => m.Type).HasMaxLength(500).IsRequired();
            b.Property(m => m.Content).IsRequired();
        });

        base.OnModelCreating(modelBuilder);
    }
}
