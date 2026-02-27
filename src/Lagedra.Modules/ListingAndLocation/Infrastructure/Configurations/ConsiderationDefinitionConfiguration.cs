using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class ConsiderationDefinitionConfiguration : IEntityTypeConfiguration<PropertyConsiderationDefinition>
{
    public void Configure(EntityTypeBuilder<PropertyConsiderationDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("consideration_definitions");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();
        builder.Property(c => c.IconKey).HasMaxLength(100).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.SortOrder).IsRequired();
        builder.HasIndex(c => c.SortOrder);
    }
}
