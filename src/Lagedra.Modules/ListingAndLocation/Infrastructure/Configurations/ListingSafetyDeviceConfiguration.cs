using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class ListingSafetyDeviceConfiguration : IEntityTypeConfiguration<ListingSafetyDevice>
{
    public void Configure(EntityTypeBuilder<ListingSafetyDevice> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("listing_safety_devices");
        builder.HasKey(ls => new { ls.ListingId, ls.SafetyDeviceDefinitionId });
        builder.HasIndex(ls => ls.SafetyDeviceDefinitionId);
        builder.HasOne(ls => ls.SafetyDeviceDefinition).WithMany().HasForeignKey(ls => ls.SafetyDeviceDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
