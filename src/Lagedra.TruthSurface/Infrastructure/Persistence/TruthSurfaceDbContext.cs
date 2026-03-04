using Lagedra.Infrastructure.Persistence;
using Lagedra.SharedKernel.Time;
using Lagedra.TruthSurface.Domain;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.TruthSurface.Infrastructure.Persistence;

public sealed class TruthSurfaceDbContext(
    DbContextOptions<TruthSurfaceDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "truth_surface";

    public DbSet<TruthSnapshot> Snapshots => Set<TruthSnapshot>();
    public DbSet<CryptographicProof> CryptographicProofs => Set<CryptographicProof>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TruthSurfaceDbContext).Assembly);
    }
}
