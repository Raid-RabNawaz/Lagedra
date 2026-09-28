using Lagedra.Modules.ChannelIntegration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Configurations;

public sealed class ChannelListingMapConfiguration : IEntityTypeConfiguration<ChannelListingMap>
{
    public void Configure(EntityTypeBuilder<ChannelListingMap> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("channel_listing_maps");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.ConnectionId).IsRequired();
        builder.Property(m => m.ProviderListingId).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Title).HasMaxLength(1000);
        builder.Property(m => m.SyncPriceFromPms).IsRequired().HasDefaultValue(false);
        builder.Property(m => m.SyncCalendarFromPms).IsRequired().HasDefaultValue(false);
        builder.Property(m => m.PmsPriceBasis).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.PmsSourceAmountCents);

        builder.HasIndex(m => m.ConnectionId);
        builder.HasIndex(m => new { m.ConnectionId, m.ProviderListingId }).IsUnique();
        builder.HasIndex(m => m.ListingId);
    }
}
