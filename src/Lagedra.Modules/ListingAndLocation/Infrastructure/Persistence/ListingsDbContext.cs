using Lagedra.Infrastructure.Persistence;
using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.SharedKernel.Time;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;

public sealed class ListingsDbContext(
    DbContextOptions<ListingsDbContext> options,
    IClock clock)
    : BaseDbContext(options, clock)
{
    protected override string ModuleSchema => "listings";

    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<AmenityDefinition> AmenityDefinitions => Set<AmenityDefinition>();
    public DbSet<SafetyDeviceDefinition> SafetyDeviceDefinitions => Set<SafetyDeviceDefinition>();
    public DbSet<PropertyConsiderationDefinition> ConsiderationDefinitions => Set<PropertyConsiderationDefinition>();
    public DbSet<ListingAmenity> ListingAmenities => Set<ListingAmenity>();
    public DbSet<ListingSafetyDevice> ListingSafetyDevices => Set<ListingSafetyDevice>();
    public DbSet<ListingConsideration> ListingConsiderations => Set<ListingConsideration>();
    public DbSet<ListingAvailabilityBlock> ListingAvailabilityBlocks => Set<ListingAvailabilityBlock>();
    public DbSet<ListingPhoto> ListingPhotos => Set<ListingPhoto>();
    public DbSet<SavedListing> SavedListings => Set<SavedListing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ListingsDbContext).Assembly);
    }
}
