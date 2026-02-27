using Lagedra.Modules.ComplianceMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lagedra.Modules.ComplianceMonitoring.Infrastructure.Configurations;

public sealed class MonitoredComplianceSignalConfiguration : IEntityTypeConfiguration<MonitoredComplianceSignal>
{
    public void Configure(EntityTypeBuilder<MonitoredComplianceSignal> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("monitored_compliance_signals");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DealId).IsRequired();
        builder.HasIndex(s => s.DealId);

        builder.Property(s => s.SignalType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Source)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.ReceivedAt).IsRequired();
    }
}
