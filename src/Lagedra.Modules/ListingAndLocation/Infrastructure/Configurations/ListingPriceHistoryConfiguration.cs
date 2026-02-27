using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class ListingPriceHistoryConfiguration : IEntityTypeConfiguration<ListingPriceHistory>
{
    public void Configure(EntityTypeBuilder<ListingPriceHistory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("listing_price_history", "listings");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.ListingId).IsRequired();
        builder.Property(h => h.MonthlyRentCents).IsRequired();
        builder.Property(h => h.EffectiveFrom).IsRequired();
        builder.Property(h => h.EffectiveTo);

        builder.HasIndex(h => h.ListingId);
        builder.HasIndex(h => new { h.ListingId, h.EffectiveFrom });
    }
}
