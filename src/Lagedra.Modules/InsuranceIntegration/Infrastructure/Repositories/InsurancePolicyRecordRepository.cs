using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Domain.Entities;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Repositories;

public sealed class InsurancePolicyRecordRepository(InsuranceDbContext dbContext)
    : IInsurancePolicyRecordStore
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

    public void AddAttempt(InsurancePolicyRecord record, InsuranceVerificationAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(record);

        record.AddAttempt(attempt);

        // EF infers the state of an entity discovered inside a tracked parent's
        // collection from whether its key is set. The attempt Id is assigned in
        // the constructor, so without this the save emits an UPDATE against a
        // row that does not exist and the whole transaction rolls back.
        dbContext.VerificationAttempts.Add(attempt);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
