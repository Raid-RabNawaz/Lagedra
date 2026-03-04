using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.Arbitration.Domain.Aggregates;
using Lagedra.Modules.Arbitration.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Infrastructure.Persistence;

public sealed class ArbitrationDbContext(
    DbContextOptions<ArbitrationDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "arbitration";

    public DbSet<ArbitrationCase> ArbitrationCases => Set<ArbitrationCase>();
    public DbSet<EvidenceSlot> EvidenceSlots => Set<EvidenceSlot>();
    public DbSet<ArbitratorAssignment> ArbitratorAssignments => Set<ArbitratorAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArbitrationDbContext).Assembly);
    }
}
