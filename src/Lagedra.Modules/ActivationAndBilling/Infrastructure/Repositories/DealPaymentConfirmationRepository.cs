using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Domain.Interfaces;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Repositories;

public sealed class DealPaymentConfirmationRepository(BillingDbContext dbContext)
    : IDealPaymentConfirmationRepository
{
    public async Task<DealPaymentConfirmation?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<DealPaymentConfirmation?> GetByDealIdAsync(Guid dealId, CancellationToken ct = default) =>
        await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == dealId, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<DealPaymentConfirmation>> GetPendingExpiredAsync(
        DateTime cutoff, CancellationToken ct = default) =>
        await dbContext.DealPaymentConfirmations
            .Where(c => c.Status == PaymentConfirmationStatus.Pending
                && c.GracePeriodExpiresAt <= cutoff)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public void Add(DealPaymentConfirmation confirmation) =>
        dbContext.DealPaymentConfirmations.Add(confirmation);

    public void Update(DealPaymentConfirmation confirmation) =>
        dbContext.Entry(confirmation).State = EntityState.Modified;
}
