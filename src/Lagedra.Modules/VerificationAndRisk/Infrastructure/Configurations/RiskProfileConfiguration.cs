using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Lagedra.Modules.VerificationAndRisk.Domain.Aggregates;

namespace Lagedra.Modules.VerificationAndRisk.Infrastructure.Configurations;

public sealed class RiskProfileConfiguration : IEntityTypeConfiguration<RiskProfile>
{
    public void Configure(EntityTypeBuilder<RiskProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("risk_profiles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantUserId).IsRequired();
        builder.HasIndex(r => r.TenantUserId).IsUnique();

        builder.Property(r => r.VerificationClass)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(r => r.Confidence, ci =>
        {
            ci.Property(c => c.Level)
                .HasConversion<string>()
                .HasColumnName("confidence_level")
                .HasMaxLength(20)
                .IsRequired();

            ci.Property(c => c.Reason)
                .HasColumnName("confidence_reason")
                .HasMaxLength(500)
                .IsRequired();
        });

        builder.Property(r => r.DepositBandLowCents).IsRequired();
        builder.Property(r => r.DepositBandHighCents).IsRequired();
        builder.Property(r => r.ComputedAt).IsRequired();
        builder.Property(r => r.InputHash).HasMaxLength(64);

        builder.Ignore(r => r.DomainEvents);
    }
}
