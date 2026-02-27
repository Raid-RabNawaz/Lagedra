using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.PartnerNetwork.Domain.Aggregates;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;

public sealed class PartnerDbContext(
    DbContextOptions<PartnerDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "partner_network";

    public DbSet<PartnerOrganization> Organizations => Set<PartnerOrganization>();
    public DbSet<PartnerMember> Members => Set<PartnerMember>();
    public DbSet<ReferralLink> ReferralLinks => Set<ReferralLink>();
    public DbSet<ReferralRedemption> ReferralRedemptions => Set<ReferralRedemption>();
    public DbSet<DirectReservation> DirectReservations => Set<DirectReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PartnerDbContext).Assembly);
    }
}
