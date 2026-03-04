using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
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
        builder.Property(l => l.Description).HasMaxLength(5000).IsRequired();
        builder.Property(l => l.MonthlyRentCents).IsRequired();
        builder.Property(l => l.InsuranceRequired).IsRequired();
        builder.Property(l => l.Bedrooms).IsRequired();
        builder.Property(l => l.Bathrooms).HasColumnType("decimal(3,1)").IsRequired();
        builder.Property(l => l.SquareFootage);
        builder.Property(l => l.MaxDepositCents).IsRequired();
        builder.Property(l => l.SuggestedDepositLowCents);
        builder.Property(l => l.SuggestedDepositHighCents);
        builder.Property(l => l.JurisdictionCode).HasMaxLength(50);
        builder.Property(l => l.InstantBookingEnabled).HasDefaultValue(false);
        builder.Property(l => l.VirtualTourUrl).HasMaxLength(2000);

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
