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

        // Headcount the tenant declared at submission time. Required, with
        // a default of 1 so existing rows backfill cleanly when the
        // AddBookingRequestGuestCountAndMessage migration runs.
        builder.Property(a => a.GuestCount)
            .HasDefaultValue(1)
            .IsRequired();

        // Optional cover note (Airbnb-style "send the host a message"). Null
        // when the tenant skipped the field. Capped at the same length the
        // domain factory enforces so an out-of-bounds value would fail
        // EF validation before it ever hit the database.
        builder.Property(a => a.Message)
            .HasMaxLength(DealApplication.MessageMaxLength)
            .IsRequired(false);

        builder.Property(a => a.DepositAmountCents);
        builder.Property(a => a.InsuranceFeeCents);
        builder.Property(a => a.FirstMonthRentCents);
        builder.Property(a => a.PartnerOrganizationId);
        builder.Property(a => a.IsPartnerReferred).IsRequired();
        builder.Property(a => a.Source)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.HasIndex(a => a.Source);
        builder.Property(a => a.JurisdictionWarning).HasMaxLength(2000);

        builder.Property(a => a.TruthSurfaceSnapshotId);
        builder.HasIndex(a => a.TruthSurfaceSnapshotId);

        // Phase 16.9 — Stripe `pm_…` payment-method id; comfortably bounded.
        builder.Property(a => a.StripePaymentMethodId)
            .HasMaxLength(64)
            .IsRequired(false);

        builder.HasIndex(a => a.DealId)
            .HasFilter("\"DealId\" IS NOT NULL")
            .IsUnique();

        builder.Ignore(a => a.DomainEvents);
    }
}
