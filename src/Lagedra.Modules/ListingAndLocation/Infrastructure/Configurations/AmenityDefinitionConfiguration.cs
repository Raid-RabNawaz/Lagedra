using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class AmenityDefinitionConfiguration : IEntityTypeConfiguration<AmenityDefinition>
{
    public void Configure(EntityTypeBuilder<AmenityDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("amenity_definitions");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(a => a.Name).IsUnique();
        builder.Property(a => a.Category).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.IconKey).HasMaxLength(100).IsRequired();
        builder.Property(a => a.IsActive).IsRequired();
        builder.Property(a => a.SortOrder).IsRequired();
        builder.HasIndex(a => new { a.Category, a.SortOrder });
    }
}
