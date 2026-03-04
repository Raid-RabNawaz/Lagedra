using Lagedra.Modules.Privacy.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.Privacy.Infrastructure.Configurations;

public sealed class ConsentRecordConfiguration : IEntityTypeConfiguration<ConsentRecord>
{
    public void Configure(EntityTypeBuilder<ConsentRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("consent_records");
        builder.HasKey(cr => cr.Id);

        builder.Property(cr => cr.UserConsentId).IsRequired();

        builder.Property(cr => cr.ConsentType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(cr => cr.IpAddress).HasMaxLength(45).IsRequired();
        builder.Property(cr => cr.UserAgent).HasMaxLength(512).IsRequired();
    }
}
