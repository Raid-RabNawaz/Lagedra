using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;
using Lagedra.Modules.VerificationAndRisk.Domain.Aggregates;

namespace Lagedra.Modules.VerificationAndRisk.Infrastructure.Persistence;

public sealed class RiskDbContext(
    DbContextOptions<RiskDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "risk";

    public DbSet<RiskProfile> RiskProfiles => Set<RiskProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RiskDbContext).Assembly);
    }
}
