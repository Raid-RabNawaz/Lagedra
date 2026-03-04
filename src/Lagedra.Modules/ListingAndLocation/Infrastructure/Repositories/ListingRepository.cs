using Lagedra.Modules.ListingAndLocation.Domain.Aggregates;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Repositories;

public sealed class ListingRepository(ListingsDbContext dbContext)
{
    public async Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Listings
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Listing>> GetByLandlordAsync(
        Guid landlordUserId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Listings
            .AsNoTracking()
            .Where(l => l.LandlordUserId == landlordUserId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Listing>> GetListingsWithoutJurisdictionAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Listings
            .Where(l => l.JurisdictionCode == null
                && l.PreciseAddress != null
                && (l.Status == ListingStatus.Published || l.Status == ListingStatus.Activated))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(Listing listing) =>
        dbContext.Listings.Add(listing);
}
