using Lagedra.Modules.JurisdictionPacks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Configurations;

public sealed class EvidenceScheduleConfiguration : IEntityTypeConfiguration<EvidenceSchedule>
{
    public void Configure(EntityTypeBuilder<EvidenceSchedule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("evidence_schedules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.VersionId).IsRequired();
        builder.Property(r => r.Category).HasMaxLength(200).IsRequired();
        builder.Property(r => r.MinimumRequirements).IsRequired();
    }
}
