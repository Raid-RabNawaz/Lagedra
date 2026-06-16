using Lagedra.Modules.ChannelIntegration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Configurations;

public sealed class ChannelSyncCursorConfiguration : IEntityTypeConfiguration<ChannelSyncCursor>
{
    public void Configure(EntityTypeBuilder<ChannelSyncCursor> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("channel_sync_cursors");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.ConnectionId).IsRequired();
        builder.Property(c => c.CursorKind).HasMaxLength(100).IsRequired();
        builder.Property(c => c.LastChangedAtUtc).IsRequired();

        builder.HasIndex(c => new { c.ConnectionId, c.CursorKind }).IsUnique();
    }
}
