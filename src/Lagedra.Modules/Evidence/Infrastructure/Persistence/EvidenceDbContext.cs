using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.Evidence.Domain.Aggregates;
using Lagedra.Modules.Evidence.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Evidence.Infrastructure.Persistence;

public sealed class EvidenceDbContext(
    DbContextOptions<EvidenceDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "evidence";

    public DbSet<EvidenceManifest> Manifests => Set<EvidenceManifest>();
    public DbSet<EvidenceUpload> Uploads => Set<EvidenceUpload>();
    public DbSet<MalwareScanResult> ScanResults => Set<MalwareScanResult>();
    public DbSet<MetadataStrippingLog> MetadataStrippingLogs => Set<MetadataStrippingLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EvidenceDbContext).Assembly);
    }
}
