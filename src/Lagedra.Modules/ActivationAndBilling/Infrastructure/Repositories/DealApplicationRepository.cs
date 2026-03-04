using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Repositories;

public sealed class DealApplicationRepository(BillingDbContext dbContext)
{
    public async Task<DealApplication?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.DealApplications
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<DealApplication>> GetByListingIdAsync(
        Guid listingId, CancellationToken cancellationToken = default) =>
        await dbContext.DealApplications
            .AsNoTracking()
            .Where(a => a.ListingId == listingId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(DealApplication application) =>
        dbContext.DealApplications.Add(application);
}
