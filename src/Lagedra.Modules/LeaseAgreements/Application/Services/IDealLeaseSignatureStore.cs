using Lagedra.Modules.LeaseAgreements.Domain.Entities;
using Lagedra.Modules.LeaseAgreements.Domain.Enums;

namespace Lagedra.Modules.LeaseAgreements.Application.Services;

public interface IDealLeaseSignatureStore
{
    Task<IReadOnlyList<DealLeaseSignature>> ListByDealAsync(Guid dealId, CancellationToken ct = default);

    Task<DealLeaseSignature?> GetAsync(
        Guid dealId,
        DealLeaseSignerRole role,
        CancellationToken ct = default);

    Task SaveAsync(DealLeaseSignature signature, CancellationToken ct = default);
}
