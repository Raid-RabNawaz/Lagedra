using Lagedra.Modules.Evidence.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Evidence.Infrastructure.Configurations;

public sealed class EvidenceManifestConfiguration : IEntityTypeConfiguration<EvidenceManifest>
{
    public void Configure(EntityTypeBuilder<EvidenceManifest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("manifests");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.DealId).IsRequired();
        builder.HasIndex(m => m.DealId);

        builder.Property(m => m.ManifestType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(m => m.HashOfAllFiles).HasMaxLength(128);

        builder.HasMany(m => m.Uploads)
            .WithOne()
            .HasForeignKey(u => u.ManifestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(m => m.DomainEvents);
    }
}
