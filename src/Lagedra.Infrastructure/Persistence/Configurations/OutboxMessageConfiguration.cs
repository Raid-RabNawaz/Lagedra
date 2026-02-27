using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configures the outbox_messages table inside a module's own schema.
/// Each module that extends BaseDbContext passes its ModuleSchema here,
/// so every module gets its own isolated outbox table:
///   truth_surface.outbox_messages
///   compliance.outbox_messages
///   activation_billing.outbox_messages  — etc.
/// This prevents cross-module row collisions in the OutboxDispatcher.
/// </summary>
public sealed class OutboxMessageConfiguration(string schema) : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("outbox_messages", schema);
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Type).HasMaxLength(500).IsRequired();
        builder.Property(m => m.Content).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(2000);
        builder.HasIndex(m => m.ProcessedAt);
    }
}
