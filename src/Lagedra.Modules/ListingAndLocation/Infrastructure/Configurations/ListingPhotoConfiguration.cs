using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class ListingPhotoConfiguration : IEntityTypeConfiguration<ListingPhoto>
{
    public void Configure(EntityTypeBuilder<ListingPhoto> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("listing_photos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(p => p.Url).HasMaxLength(2000).IsRequired();
        builder.Property(p => p.Caption).HasMaxLength(500);
        builder.Property(p => p.IsCover).IsRequired();
        builder.Property(p => p.SortOrder).IsRequired();

        builder.HasIndex(p => new { p.ListingId, p.SortOrder });
    }
}
