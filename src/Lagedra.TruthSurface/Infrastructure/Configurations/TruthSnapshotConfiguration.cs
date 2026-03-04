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

        builder.HasOne(s => s.Proof)
            .WithOne()
            .HasForeignKey<CryptographicProof>(p => p.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(s => s.DomainEvents);
    }
}
