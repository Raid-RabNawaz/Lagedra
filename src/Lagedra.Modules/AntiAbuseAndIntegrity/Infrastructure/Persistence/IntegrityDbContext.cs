using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Aggregates;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;
using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;

public sealed class IntegrityDbContext(
    DbContextOptions<IntegrityDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "integrity";

    public DbSet<AbuseCase> AbuseCases => Set<AbuseCase>();
    public DbSet<CollusionPattern> CollusionPatterns => Set<CollusionPattern>();
    public DbSet<FraudFlag> FraudFlags => Set<FraudFlag>();
    public DbSet<AccountRestriction> AccountRestrictions => Set<AccountRestriction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IntegrityDbContext).Assembly);
    }
}
