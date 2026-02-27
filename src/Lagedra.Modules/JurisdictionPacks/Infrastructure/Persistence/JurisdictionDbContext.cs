using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.JurisdictionPacks.Domain.Aggregates;
using Lagedra.Modules.JurisdictionPacks.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.JurisdictionPacks.Infrastructure.Persistence;

public sealed class JurisdictionDbContext(
    DbContextOptions<JurisdictionDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "jurisdiction";

    public DbSet<JurisdictionPack> JurisdictionPacks => Set<JurisdictionPack>();
    public DbSet<PackVersion> PackVersions => Set<PackVersion>();
    public DbSet<EffectiveDateRule> EffectiveDateRules => Set<EffectiveDateRule>();
    public DbSet<FieldGatingRule> FieldGatingRules => Set<FieldGatingRule>();
    public DbSet<EvidenceSchedule> EvidenceSchedules => Set<EvidenceSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JurisdictionDbContext).Assembly);
    }
}
