using Lagedra.Modules.ChannelIntegration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Configurations;

public sealed class ChannelBookingLinkConfiguration : IEntityTypeConfiguration<ChannelBookingLink>
{
    public void Configure(EntityTypeBuilder<ChannelBookingLink> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("channel_booking_links");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.ConnectionId).IsRequired();
        builder.Property(b => b.DealId).IsRequired();
        builder.Property(b => b.ProviderBookingId).HasMaxLength(200);

        builder.Property(b => b.SyncStatus)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.LastError).HasMaxLength(2000);

        builder.HasIndex(b => b.DealId).IsUnique();
        builder.HasIndex(b => new { b.ConnectionId, b.ProviderBookingId });
    }
}
