using Lagedra.Compliance.Domain;
using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Infrastructure.Persistence;

public sealed class ComplianceDbContext(
    DbContextOptions<ComplianceDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "compliance";

    public DbSet<Violation> Violations => Set<Violation>();
    public DbSet<TrustLedgerEntry> TrustLedgerEntries => Set<TrustLedgerEntry>();
    public DbSet<ComplianceSignal> Signals => Set<ComplianceSignal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ComplianceDbContext).Assembly);
    }
}
