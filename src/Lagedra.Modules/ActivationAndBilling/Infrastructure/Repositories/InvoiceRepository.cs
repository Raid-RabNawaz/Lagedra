using Lagedra.Modules.ActivationAndBilling.Domain.Entities;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Repositories;

public sealed class InvoiceRepository(BillingDbContext dbContext)
{
    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Invoice>> GetByBillingAccountIdAsync(
        Guid billingAccountId, CancellationToken cancellationToken = default) =>
        await dbContext.Invoices
            .AsNoTracking()
            .Where(i => i.BillingAccountId == billingAccountId)
            .OrderByDescending(i => i.PeriodStart)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(Invoice invoice) =>
        dbContext.Invoices.Add(invoice);
}
