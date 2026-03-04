using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Repositories;

public sealed class InsurancePolicyRecordRepository(InsuranceDbContext dbContext)
{
    public async Task<InsurancePolicyRecord?> GetByDealIdAsync(
        Guid dealId, CancellationToken cancellationToken = default) =>
        await dbContext.PolicyRecords
            .Include(r => r.Attempts)
            .FirstOrDefaultAsync(r => r.DealId == dealId, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<InsurancePolicyRecord>> GetUnknownRecordsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.PolicyRecords
            .Include(r => r.Attempts)
            .Where(r => r.State == InsuranceState.Unknown && r.UnknownSince != null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(InsurancePolicyRecord record) =>
        dbContext.PolicyRecords.Add(record);
}
