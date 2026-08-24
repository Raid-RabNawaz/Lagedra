using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Configurations;

public sealed class RentCheckInConfiguration : IEntityTypeConfiguration<RentCheckIn>
{
    public void Configure(EntityTypeBuilder<RentCheckIn> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("rent_check_ins");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.DealId).IsRequired();
        builder.Property(r => r.LandlordUserId).IsRequired();
        builder.Property(r => r.PeriodStart).IsRequired();
        builder.Property(r => r.PeriodEnd).IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.Note).HasMaxLength(500);

        // One check-in per deal per rent period — the nightly sweep relies on
        // this to stay idempotent.
        builder.HasIndex(r => new { r.DealId, r.PeriodStart }).IsUnique();
        builder.HasIndex(r => r.Status);
    }
}
