using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.Privacy.Domain.Aggregates;
using Lagedra.Modules.Privacy.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Infrastructure.Persistence;

public sealed class PrivacyDbContext(
    DbContextOptions<PrivacyDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "privacy";

    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<LegalHold> LegalHolds => Set<LegalHold>();
    public DbSet<DataExportRequest> DataExportRequests => Set<DataExportRequest>();
    public DbSet<DeletionRequest> DeletionRequests => Set<DeletionRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrivacyDbContext).Assembly);
    }
}
