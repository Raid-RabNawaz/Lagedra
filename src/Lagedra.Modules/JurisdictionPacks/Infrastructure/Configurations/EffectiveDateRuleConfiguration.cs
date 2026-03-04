using Lagedra.Modules.JurisdictionPacks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Configurations;

public sealed class EffectiveDateRuleConfiguration : IEntityTypeConfiguration<EffectiveDateRule>
{
    public void Configure(EntityTypeBuilder<EffectiveDateRule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("effective_date_rules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.VersionId).IsRequired();
        builder.Property(r => r.FieldName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.EffectiveDate).IsRequired();

        builder.HasIndex(r => new { r.VersionId, r.FieldName }).IsUnique();
    }
}
