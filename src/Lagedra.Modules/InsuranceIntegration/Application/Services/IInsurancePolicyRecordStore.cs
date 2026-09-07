using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Domain.Entities;

namespace Lagedra.Modules.InsuranceIntegration.Application.Services;

public interface IInsurancePolicyRecordStore
{
    Task<InsurancePolicyRecord?> GetByDealIdAsync(
        Guid dealId,
        CancellationToken cancellationToken = default);

    void Add(InsurancePolicyRecord record);

    /// <summary>
    /// Records a verification attempt against the aggregate and registers it
    /// for insert. Both halves belong together: attempts carry a
    /// domain-assigned Id, so adding one to a loaded record's collection alone
    /// leaves the persistence layer treating it as an existing row.
    /// </summary>
    void AddAttempt(InsurancePolicyRecord record, InsuranceVerificationAttempt attempt);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
