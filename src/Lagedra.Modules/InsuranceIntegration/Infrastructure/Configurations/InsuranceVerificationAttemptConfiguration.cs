using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Configurations;

public sealed class InsuranceVerificationAttemptConfiguration : IEntityTypeConfiguration<InsuranceVerificationAttempt>
{
    public void Configure(EntityTypeBuilder<InsuranceVerificationAttempt> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("verification_attempts");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.PolicyRecordId).IsRequired();
        builder.HasIndex(a => a.PolicyRecordId);

        builder.Property(a => a.AttemptedAt).IsRequired();

        builder.Property(a => a.Result)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(a => a.Source)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
    }
}
