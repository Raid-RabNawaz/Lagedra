using Lagedra.Compliance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Compliance.Infrastructure.Configurations;

public sealed class ViolationConfiguration : IEntityTypeConfiguration<Violation>
{
    public void Configure(EntityTypeBuilder<Violation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("violations");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.DealId).IsRequired();
        builder.HasIndex(v => v.DealId);

        builder.Property(v => v.ReportedByUserId).IsRequired();

        builder.Property(v => v.TargetUserId).IsRequired();
        builder.HasIndex(v => v.TargetUserId);

        builder.Property(v => v.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(v => v.Description).HasMaxLength(2000).IsRequired();
        builder.Property(v => v.EvidenceReference).HasMaxLength(500);
    }
}
