using Lagedra.Modules.JurisdictionPacks.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Configurations;

public sealed class DepositCapRuleConfiguration : IEntityTypeConfiguration<DepositCapRule>
{
    public void Configure(EntityTypeBuilder<DepositCapRule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("deposit_cap_rules");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.VersionId).IsRequired();
        builder.Property(r => r.JurisdictionCode).HasMaxLength(20).IsRequired();
        builder.Property(r => r.MaxMultiplier).HasPrecision(5, 2).IsRequired();
        builder.Property(r => r.ExceptionCondition).HasMaxLength(200);
        builder.Property(r => r.ExceptionMultiplier).HasPrecision(5, 2);
        builder.Property(r => r.LegalReference).HasMaxLength(500).IsRequired();

        builder.HasIndex(r => new { r.VersionId, r.JurisdictionCode });
    }
}
