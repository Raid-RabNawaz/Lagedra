using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.ChannelIntegration.Domain.Aggregates;
using Lagedra.Modules.ChannelIntegration.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ChannelIntegration.Infrastructure.Persistence;

public sealed class ChannelDbContext(
    DbContextOptions<ChannelDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "channel_integration";

    public DbSet<ChannelConnection> Connections => Set<ChannelConnection>();
    public DbSet<ChannelListingMap> ListingMaps => Set<ChannelListingMap>();
    public DbSet<ChannelBookingLink> BookingLinks => Set<ChannelBookingLink>();
    public DbSet<ChannelSyncCursor> SyncCursors => Set<ChannelSyncCursor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChannelDbContext).Assembly);
    }
}
