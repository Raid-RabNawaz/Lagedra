using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.AuditLog.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AuditLog.Infrastructure.Persistence;

public sealed class AuditDbContext(
    DbContextOptions<AuditDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "audit";

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.ToTable("audit_events");
            e.HasKey(a => a.Id);
            e.Property(a => a.EventType).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
            e.Property(a => a.EntityId).HasMaxLength(200).IsRequired();
            e.Property(a => a.Details).HasColumnType("jsonb");
            e.Property(a => a.IpAddress).HasMaxLength(45);
            e.HasIndex(a => a.UserId);
            e.HasIndex(a => a.EventType);
            e.HasIndex(a => a.Timestamp);
        });
    }
}
