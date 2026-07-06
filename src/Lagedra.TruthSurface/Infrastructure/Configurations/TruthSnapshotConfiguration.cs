using Lagedra.TruthSurface.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.TruthSurface.Infrastructure.Configurations;

public sealed class TruthSnapshotConfiguration : IEntityTypeConfiguration<TruthSnapshot>
{
    public void Configure(EntityTypeBuilder<TruthSnapshot> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("snapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DealId).IsRequired();
        builder.HasIndex(s => s.DealId);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.ProtocolVersion).HasMaxLength(20).IsRequired();
        builder.Property(s => s.JurisdictionPackVersion).HasMaxLength(20).IsRequired();
        builder.Property(s => s.CanonicalContent);
        builder.Property(s => s.Hash).HasMaxLength(128);
        builder.Property(s => s.Signature).HasMaxLength(256);

        builder.Property(s => s.IsLocked).HasDefaultValue(false).IsRequired();
        builder.Property(s => s.LockedAt);

        // Consent audit metadata (tenant at request, host at approval).
        builder.Property(s => s.TenantConsentUserId);
        builder.Property(s => s.TenantConsentAt);
        builder.Property(s => s.TenantConsentIp).HasMaxLength(64);
        builder.Property(s => s.TenantConsentUserAgent).HasMaxLength(512);
        builder.Property(s => s.TenantConsentVersion).HasMaxLength(50);

        builder.Property(s => s.HostConsentUserId);
        builder.Property(s => s.HostConsentAt);
        builder.Property(s => s.HostConsentIp).HasMaxLength(64);
        builder.Property(s => s.HostConsentUserAgent).HasMaxLength(512);
        builder.Property(s => s.HostConsentVersion).HasMaxLength(50);

        builder.HasOne(s => s.Proof)
            .WithOne()
            .HasForeignKey<CryptographicProof>(p => p.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);

        // Append-only — soft-delete columns are dropped from the schema and
        // intentionally not mapped so any code that mutates them fails fast.
        builder.Ignore(s => s.IsDeleted);
        builder.Ignore(s => s.DeletedAt);
    }
}
