using Lagedra.Modules.Evidence.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Evidence.Infrastructure.Configurations;

public sealed class EvidenceUploadConfiguration : IEntityTypeConfiguration<EvidenceUpload>
{
    public void Configure(EntityTypeBuilder<EvidenceUpload> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("uploads");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.ManifestId).IsRequired();
        builder.HasIndex(u => u.ManifestId);

        builder.Property(u => u.OriginalFileName).HasMaxLength(500).IsRequired();
        builder.Property(u => u.StorageKey).HasMaxLength(1000).IsRequired();
        builder.Property(u => u.MimeType).HasMaxLength(200).IsRequired();
        builder.Property(u => u.TimestampMetadata);

        builder.OwnsOne(u => u.FileHash, fh =>
        {
            fh.Property(h => h.Value)
                .HasColumnName("file_hash")
                .HasMaxLength(128);
        });
    }
}
