using Lagedra.TruthSurface.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.TruthSurface.Infrastructure.Configurations;

public sealed class CryptographicProofConfiguration : IEntityTypeConfiguration<CryptographicProof>
{
    public void Configure(EntityTypeBuilder<CryptographicProof> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("cryptographic_proofs");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.SnapshotId).IsRequired();
        builder.Property(p => p.Hash).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Signature).HasMaxLength(256).IsRequired();
    }
}
