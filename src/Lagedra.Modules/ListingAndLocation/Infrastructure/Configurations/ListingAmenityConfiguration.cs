using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class ListingAmenityConfiguration : IEntityTypeConfiguration<ListingAmenity>
{
    public void Configure(EntityTypeBuilder<ListingAmenity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("listing_amenities");
        builder.HasKey(la => new { la.ListingId, la.AmenityDefinitionId });
        builder.HasIndex(la => la.AmenityDefinitionId);
        builder.HasOne(la => la.AmenityDefinition).WithMany().HasForeignKey(la => la.AmenityDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
