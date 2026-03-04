using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class SavedListingConfiguration : IEntityTypeConfiguration<SavedListing>
{
    public void Configure(EntityTypeBuilder<SavedListing> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("saved_listings");

        builder.HasKey(s => new { s.UserId, s.ListingId });

        builder.Property(s => s.CollectionId);
        builder.Property(s => s.SavedAt).IsRequired();

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.ListingId);
        builder.HasIndex(s => s.CollectionId);
    }
}
