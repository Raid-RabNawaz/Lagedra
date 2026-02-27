using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class SavedListingCollectionConfiguration : IEntityTypeConfiguration<SavedListingCollections>
{
    public void Configure(EntityTypeBuilder<SavedListingCollections> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("saved_listing_collections", "listings");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(100).IsRequired();

        builder.HasIndex(c => c.UserId);
    }
}
