using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class SafetyDeviceDefinitionConfiguration : IEntityTypeConfiguration<SafetyDeviceDefinition>
{
    public void Configure(EntityTypeBuilder<SafetyDeviceDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("safety_device_definitions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();
        builder.Property(s => s.IconKey).HasMaxLength(100).IsRequired();
        builder.Property(s => s.IsActive).IsRequired();
        builder.Property(s => s.SortOrder).IsRequired();
        builder.HasIndex(s => s.SortOrder);
    }
}
