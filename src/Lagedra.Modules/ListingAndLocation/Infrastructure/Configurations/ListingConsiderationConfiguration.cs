using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class ListingConsiderationConfiguration : IEntityTypeConfiguration<ListingConsideration>
{
    public void Configure(EntityTypeBuilder<ListingConsideration> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("listing_considerations");
        builder.HasKey(lc => new { lc.ListingId, lc.ConsiderationDefinitionId });
        builder.HasIndex(lc => lc.ConsiderationDefinitionId);
        builder.HasOne(lc => lc.ConsiderationDefinition).WithMany().HasForeignKey(lc => lc.ConsiderationDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}
