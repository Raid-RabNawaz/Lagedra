using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Configurations;

public sealed class InsurancePolicyRecordConfiguration : IEntityTypeConfiguration<InsurancePolicyRecord>
{
    public void Configure(EntityTypeBuilder<InsurancePolicyRecord> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("policy_records");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantUserId).IsRequired();
        builder.Property(r => r.DealId).IsRequired();
        builder.HasIndex(r => r.DealId).IsUnique();

        builder.Property(r => r.State)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(r => r.Provider).HasMaxLength(200);
        builder.Property(r => r.PolicyNumber).HasMaxLength(100);
        builder.Property(r => r.CoverageScope).HasMaxLength(500);

        builder.HasMany(r => r.Attempts)
            .WithOne()
            .HasForeignKey(a => a.PolicyRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(r => r.DomainEvents);
    }
}
