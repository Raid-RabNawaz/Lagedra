using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Configurations;

public sealed class ChannelConnectionConfiguration : IEntityTypeConfiguration<ChannelConnection>
{
    public void Configure(EntityTypeBuilder<ChannelConnection> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("channel_connections");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.HostUserId).IsRequired();
        builder.Property(c => c.ProviderKey).HasMaxLength(100).IsRequired();
        builder.Property(c => c.ExternalAccountId).HasMaxLength(200).IsRequired();
        builder.Property(c => c.DisplayName).HasMaxLength(500).IsRequired();
        builder.Property(c => c.Username).HasMaxLength(500);
        builder.Property(c => c.EncryptedSecret).HasMaxLength(8000);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.LastError).HasMaxLength(2000);

        builder.HasIndex(c => c.HostUserId);
        builder.HasIndex(c => new { c.HostUserId, c.ProviderKey, c.ExternalAccountId }).IsUnique();

        builder.Ignore(c => c.DomainEvents);
    }
}
