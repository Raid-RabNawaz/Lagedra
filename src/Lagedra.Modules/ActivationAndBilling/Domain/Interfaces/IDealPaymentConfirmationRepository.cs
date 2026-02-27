using Lagedra.Modules.ActivationAndBilling.Domain.Aggregates;

namespace Lagedra.Modules.ActivationAndBilling.Domain.Interfaces;

public interface IDealPaymentConfirmationRepository
{
    Task<DealPaymentConfirmation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DealPaymentConfirmation?> GetByDealIdAsync(Guid dealId, CancellationToken ct = default);
    Task<IReadOnlyList<DealPaymentConfirmation>> GetPendingExpiredAsync(DateTime cutoff, CancellationToken ct = default);
    void Add(DealPaymentConfirmation confirmation);
    void Update(DealPaymentConfirmation confirmation);
}
