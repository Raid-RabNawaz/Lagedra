using Lagedra.Modules.ComplianceMonitoring.Domain.Entities;
using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;

public sealed class ComplianceMonitoringDbContext(
    DbContextOptions<ComplianceMonitoringDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "compliance_monitoring";

    public DbSet<MonitoredViolation> Violations => Set<MonitoredViolation>();
    public DbSet<MonitoredComplianceSignal> Signals => Set<MonitoredComplianceSignal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ComplianceMonitoringDbContext).Assembly);
    }
}
