using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Configurations;

public sealed class KycDocumentConfiguration : IEntityTypeConfiguration<KycDocument>
{
    public void Configure(EntityTypeBuilder<KycDocument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("kyc_documents");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.UserId).IsRequired();
        builder.HasIndex(d => d.UserId);

        builder.Property(d => d.DocumentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(d => d.MimeType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.UploadedAt).IsRequired();
    }
}
