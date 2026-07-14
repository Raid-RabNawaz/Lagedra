using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.LeaseAgreements.Domain.Aggregates;
using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.LeaseAgreements.Infrastructure.Persistence;

public sealed class LeaseAgreementDbContext(
    DbContextOptions<LeaseAgreementDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "lease_agreements";

    public DbSet<LeaseAgreementTemplate> Templates => Set<LeaseAgreementTemplate>();
    public DbSet<LeaseTemplateVersion> Versions => Set<LeaseTemplateVersion>();
    public DbSet<DealLeaseDocumentEntity> DealDocuments => Set<DealLeaseDocumentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LeaseAgreementDbContext).Assembly);
    }
}
