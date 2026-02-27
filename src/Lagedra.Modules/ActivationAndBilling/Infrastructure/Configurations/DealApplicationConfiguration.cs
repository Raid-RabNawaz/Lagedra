using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Configurations;

public sealed class DealApplicationConfiguration : IEntityTypeConfiguration<DealApplication>
{
    public void Configure(EntityTypeBuilder<DealApplication> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("deal_applications");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ListingId).IsRequired();
        builder.HasIndex(a => a.ListingId);

        builder.Property(a => a.TenantUserId).IsRequired();
        builder.Property(a => a.LandlordUserId).IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.SubmittedAt).IsRequired();
        builder.Property(a => a.RequestedCheckIn).IsRequired();
        builder.Property(a => a.RequestedCheckOut).IsRequired();
        builder.Property(a => a.StayDurationDays).IsRequired();

        builder.Property(a => a.DepositAmountCents);
        builder.Property(a => a.InsuranceFeeCents);
        builder.Property(a => a.FirstMonthRentCents);
        builder.Property(a => a.PartnerOrganizationId);
        builder.Property(a => a.IsPartnerReferred).IsRequired();
        builder.Property(a => a.JurisdictionWarning).HasMaxLength(2000);

        builder.HasIndex(a => a.DealId)
            .HasFilter("\"DealId\" IS NOT NULL")
            .IsUnique();

        builder.Ignore(a => a.DomainEvents);
    }
}
