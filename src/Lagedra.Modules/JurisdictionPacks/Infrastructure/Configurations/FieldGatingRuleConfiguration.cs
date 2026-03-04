using Lagedra.Modules.JurisdictionPacks.Domain.Entities;
using Lagedra.Modules.JurisdictionPacks.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Configurations;

public sealed class FieldGatingRuleConfiguration : IEntityTypeConfiguration<FieldGatingRule>
{
    public void Configure(EntityTypeBuilder<FieldGatingRule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("field_gating_rules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.VersionId).IsRequired();
        builder.Property(r => r.FieldName).HasMaxLength(200).IsRequired();

        builder.Property(r => r.GatingType)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(r => r.Value).HasMaxLength(500).IsRequired();
        builder.Property(r => r.Condition).HasMaxLength(1000);
    }
}
