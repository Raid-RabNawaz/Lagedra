using Lagedra.Modules.ComplianceMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ComplianceMonitoring.Infrastructure.Configurations;

public sealed class MonitoredViolationConfiguration : IEntityTypeConfiguration<MonitoredViolation>
{
    public void Configure(EntityTypeBuilder<MonitoredViolation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("monitored_violations");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.DealId).IsRequired();
        builder.HasIndex(v => v.DealId);

        builder.Property(v => v.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(v => v.DetectedAt).IsRequired();

        builder.Ignore(v => v.DomainEvents);
    }
}
