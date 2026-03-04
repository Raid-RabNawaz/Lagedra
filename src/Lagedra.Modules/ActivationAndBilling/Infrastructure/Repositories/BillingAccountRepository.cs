using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Repositories;

public sealed class BillingAccountRepository(BillingDbContext dbContext)
{
    public async Task<BillingAccount?> GetByDealIdAsync(
        Guid dealId, CancellationToken cancellationToken = default) =>
        await dbContext.BillingAccounts
            .Include(b => b.Invoices)
            .FirstOrDefaultAsync(b => b.DealId == dealId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<BillingAccount?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.BillingAccounts
            .Include(b => b.Invoices)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public void Add(BillingAccount account) =>
        dbContext.BillingAccounts.Add(account);
}
