using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Configurations;

public sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("listings");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.LandlordUserId).IsRequired();
        builder.HasIndex(l => l.LandlordUserId);

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.PropertyType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Title).HasMaxLength(500).IsRequired();
        // Unbounded text: channel imports (Hostaway/Guesty/OwnerRez) carry
        // descriptions well past 5000 chars — the old varchar(5000) made the
        // content sync fail twice a day for every affected listing (22001:
        // value too long).
        builder.Property(l => l.Description).IsRequired();
        builder.Property(l => l.MonthlyRentCents).IsRequired();
        builder.Property(l => l.Bedrooms).IsRequired();
        builder.Property(l => l.Bathrooms).HasColumnType("decimal(3,1)").IsRequired();
        builder.Property(l => l.SquareFootage);
        builder.Property(l => l.MaxDepositCents).IsRequired();
        builder.Property(l => l.SuggestedDepositLowCents);
        builder.Property(l => l.SuggestedDepositHighCents);
        builder.Property(l => l.DefaultDepositCents);
        builder.Property(l => l.DepositUnverifiedCents);
        builder.Property(l => l.DepositBackgroundVerifiedCents);
        builder.Property(l => l.DepositPartnerGuaranteedCents);
        builder.Property(l => l.JurisdictionCode).HasMaxLength(50);
        builder.Property(l => l.InstantBookingEnabled).HasDefaultValue(false);
        builder.Property(l => l.AcceptsPartnerDirectReservations).HasDefaultValue(true);
        builder.Property(l => l.VirtualTourUrl).HasMaxLength(2000);

        builder.Property(l => l.ManagerRole)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ListingManagerRole.Owner)
            .IsRequired();
        builder.Property(l => l.HomeOwnerUserId);
        builder.HasIndex(l => l.HomeOwnerUserId);
        builder.Property(l => l.IncludeBrokerClause).HasDefaultValue(false);

        builder.Property(l => l.RejectionReason).HasMaxLength(2000);
        builder.Property(l => l.ReviewedAt);
        builder.Property(l => l.ReviewedByUserId);
        builder.Property(l => l.SubmittedForReviewAt);
        builder.Property(l => l.AddedVia)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ListingAddedVia.Manual)
            .IsRequired();
        builder.Property(l => l.AddedViaDetail).HasMaxLength(200);

        builder.Property(l => l.LeaseAgreementSource)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(LeaseAgreementSource.LagedraTemplate)
            .IsRequired();

        builder.OwnsOne(l => l.StayRange, stay =>
        {
            stay.Property(s => s.MinDays).HasColumnName("stay_min_days");
            stay.Property(s => s.MaxDays).HasColumnName("stay_max_days");
        });

        builder.OwnsOne(l => l.ApproxGeoPoint, geo =>
        {
            geo.Property(g => g.Latitude).HasColumnName("approx_latitude");
            geo.Property(g => g.Longitude).HasColumnName("approx_longitude");
        });

        builder.OwnsOne(l => l.PreciseAddress, addr =>
        {
            addr.Property(a => a.Street).HasColumnName("precise_street").HasMaxLength(500);
            addr.Property(a => a.City).HasColumnName("precise_city").HasMaxLength(200);
            addr.Property(a => a.State).HasColumnName("precise_state").HasMaxLength(100);
            addr.Property(a => a.ZipCode).HasColumnName("precise_zip_code").HasMaxLength(20);
            addr.Property(a => a.Country).HasColumnName("precise_country").HasMaxLength(100);
        });

        builder.OwnsOne(l => l.HouseRules, hr =>
        {
            hr.Property(h => h.CheckInTime).HasColumnName("house_rules_check_in_time");
            hr.Property(h => h.CheckOutTime).HasColumnName("house_rules_check_out_time");
            hr.Property(h => h.MaxGuests).HasColumnName("house_rules_max_guests");
            hr.Property(h => h.PetsAllowed).HasColumnName("house_rules_pets_allowed");
            hr.Property(h => h.PetsNotes).HasColumnName("house_rules_pets_notes").HasMaxLength(500);
            hr.Property(h => h.SmokingAllowed).HasColumnName("house_rules_smoking_allowed");
            hr.Property(h => h.PartiesAllowed).HasColumnName("house_rules_parties_allowed");
            hr.Property(h => h.QuietHoursStart).HasColumnName("house_rules_quiet_hours_start");
            hr.Property(h => h.QuietHoursEnd).HasColumnName("house_rules_quiet_hours_end");
            hr.Property(h => h.LeavingInstructions).HasColumnName("house_rules_leaving_instructions").HasMaxLength(2000);
            hr.Property(h => h.AdditionalRules).HasColumnName("house_rules_additional_rules").HasMaxLength(2000);
        });

        builder.OwnsOne(l => l.LeaseTerms, lt =>
        {
            lt.Property(t => t.RentDueDayOfMonth).HasColumnName("lease_rent_due_day");
            lt.Property(t => t.NsfFirstFeeCents).HasColumnName("lease_nsf_first_fee_cents");
            lt.Property(t => t.NsfSubsequentFeeCents).HasColumnName("lease_nsf_subsequent_fee_cents");
            lt.Property(t => t.LateFeePercent).HasColumnName("lease_late_fee_percent").HasPrecision(5, 2);
            lt.Property(t => t.LateFeeGraceDays).HasColumnName("lease_late_fee_grace_days");
            lt.Property(t => t.UtilitiesResponsibility).HasColumnName("lease_utilities_responsibility").HasMaxLength(500);
            lt.Property(t => t.YardMaintenanceByTenant).HasColumnName("lease_yard_maintenance_by_tenant");
            lt.Property(t => t.Furnished).HasColumnName("lease_furnished");
            lt.Property(t => t.IncludedAppliancesNotes).HasColumnName("lease_included_appliances").HasMaxLength(500);
            lt.Property(t => t.KeyCount).HasColumnName("lease_key_count");
            lt.Property(t => t.MailboxKeyCount).HasColumnName("lease_mailbox_key_count");
            lt.Property(t => t.KeyReplacementFeeCents).HasColumnName("lease_key_replacement_fee_cents");
            lt.Property(t => t.LockoutFeeCents).HasColumnName("lease_lockout_fee_cents");
            lt.Property(t => t.ParkingSpaceCount).HasColumnName("lease_parking_space_count");
            lt.Property(t => t.ParkingDescription).HasColumnName("lease_parking_description").HasMaxLength(300);
            lt.Property(t => t.ParkingIncludedInRent).HasColumnName("lease_parking_included_in_rent");
            lt.Property(t => t.MaxGuestConsecutiveDays).HasColumnName("lease_max_guest_consecutive_days");
            lt.Property(t => t.RentersInsuranceMinLiabilityCents).HasColumnName("lease_renters_insurance_min_cents");
            lt.Property(t => t.EarlyTerminationFeeMonths).HasColumnName("lease_early_termination_fee_months");
            lt.Property(t => t.BuiltBefore1978).HasColumnName("lease_built_before_1978");
            lt.Property(t => t.LeadPaintKnowledge).HasColumnName("lease_lead_paint_knowledge").HasMaxLength(1000);
            lt.Property(t => t.RentCapJustCauseExempt).HasColumnName("lease_rent_cap_just_cause_exempt");
            lt.Property(t => t.PaymentMethods).HasColumnName("lease_payment_methods").HasMaxLength(500);
        });

        builder.OwnsOne(l => l.CustomLeaseDocument, cld =>
        {
            cld.Property(d => d.StorageKey).HasColumnName("custom_lease_storage_key").HasMaxLength(1000);
            cld.Property(d => d.FileName).HasColumnName("custom_lease_file_name").HasMaxLength(300);
            cld.Property(d => d.ContentType).HasColumnName("custom_lease_content_type").HasMaxLength(200);
            cld.Property(d => d.SizeBytes).HasColumnName("custom_lease_size_bytes");
            cld.Property(d => d.ContentHash).HasColumnName("custom_lease_content_hash").HasMaxLength(128);
            cld.Property(d => d.UploadedAtUtc).HasColumnName("custom_lease_uploaded_at");
        });

        builder.OwnsOne(l => l.CancellationPolicy, cp =>
        {
            cp.Property(c => c.Type).HasColumnName("cancellation_policy_type").HasConversion<string>().HasMaxLength(50);
            cp.Property(c => c.FreeCancellationDays).HasColumnName("cancellation_free_days");
            cp.Property(c => c.PartialRefundPercent).HasColumnName("cancellation_partial_refund_percent");
            cp.Property(c => c.PartialRefundDays).HasColumnName("cancellation_partial_refund_days");
            cp.Property(c => c.CustomTerms).HasColumnName("cancellation_custom_terms").HasMaxLength(2000);
        });

        builder.HasMany(l => l.Amenities).WithOne().HasForeignKey(la => la.ListingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(l => l.SafetyDevices).WithOne().HasForeignKey(ls => ls.ListingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(l => l.Considerations).WithOne().HasForeignKey(lc => lc.ListingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(l => l.AvailabilityBlocks).WithOne().HasForeignKey(ab => ab.ListingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(l => l.Photos).WithOne().HasForeignKey(p => p.ListingId).OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Amenities).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_amenities");
        builder.Navigation(l => l.SafetyDevices).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_safetyDevices");
        builder.Navigation(l => l.Considerations).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_considerations");
        builder.Navigation(l => l.AvailabilityBlocks).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_availabilityBlocks");
        builder.Navigation(l => l.Photos).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_photos");

        builder.HasIndex(l => l.Status);
        builder.Ignore(l => l.DomainEvents);
    }
}
