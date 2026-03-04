using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class ListingAvailabilityBlockConfiguration : IEntityTypeConfiguration<ListingAvailabilityBlock>
{
    public void Configure(EntityTypeBuilder<ListingAvailabilityBlock> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("listing_availability_blocks");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.ListingId).IsRequired();
        builder.Property(b => b.DealId);
        builder.Property(b => b.CheckInDate).IsRequired();
        builder.Property(b => b.CheckOutDate).IsRequired();
        builder.Property(b => b.BlockType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(b => new { b.ListingId, b.CheckInDate, b.CheckOutDate });
        builder.HasIndex(b => b.DealId).HasFilter("\"DealId\" IS NOT NULL");
    }
}
