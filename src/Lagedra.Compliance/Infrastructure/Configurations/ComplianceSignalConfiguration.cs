using Lagedra.Compliance.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Compliance.Infrastructure.Configurations;

public sealed class ComplianceSignalConfiguration : IEntityTypeConfiguration<ComplianceSignal>
{
    public void Configure(EntityTypeBuilder<ComplianceSignal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("compliance_signals");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DealId).IsRequired();
        builder.HasIndex(s => s.DealId);

        builder.Property(s => s.SignalType).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Payload).HasMaxLength(4000);
        builder.HasIndex(s => s.Processed);
    }
}
