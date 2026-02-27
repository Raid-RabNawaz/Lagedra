using Lagedra.Modules.JurisdictionPacks.Domain.Entities;
using Lagedra.Modules.JurisdictionPacks.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Configurations;

public sealed class PackVersionConfiguration : IEntityTypeConfiguration<PackVersion>
{
    public void Configure(EntityTypeBuilder<PackVersion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("pack_versions");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.PackId).IsRequired();
        builder.Property(v => v.VersionNumber).IsRequired();

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(v => v.EffectiveDate);
        builder.Property(v => v.ApprovedAt);
        builder.Property(v => v.ApprovedBy);
        builder.Property(v => v.SecondApproverId);

        builder.HasIndex(v => new { v.PackId, v.VersionNumber }).IsUnique();

        builder.HasMany(v => v.EffectiveDateRules)
            .WithOne()
            .HasForeignKey(r => r.VersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.FieldGatingRules)
            .WithOne()
            .HasForeignKey(r => r.VersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.EvidenceSchedules)
            .WithOne()
            .HasForeignKey(r => r.VersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
