using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Configurations;

public sealed class DamageClaimConfiguration : IEntityTypeConfiguration<DamageClaim>
{
    public void Configure(EntityTypeBuilder<DamageClaim> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("damage_claims");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.DealId).IsRequired();
        builder.Property(c => c.ListingId).IsRequired();
        builder.Property(c => c.FiledByUserId).IsRequired();
        builder.Property(c => c.TenantUserId).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(4000).IsRequired();
        builder.Property(c => c.ClaimedAmountCents).IsRequired();
        builder.Property(c => c.ResolutionNotes).HasMaxLength(4000);

        builder.HasIndex(c => c.DealId).IsUnique();
        builder.HasIndex(c => c.FiledByUserId);
        builder.HasIndex(c => c.TenantUserId);
    }
}
