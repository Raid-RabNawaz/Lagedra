using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Insurance;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Services;

public sealed class InsuranceStatusProvider(InsuranceDbContext dbContext) : IInsuranceStatusProvider
{
    public async Task<bool> IsActiveAsync(Guid dealId, CancellationToken ct = default)
    {
        var record = await dbContext.PolicyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.DealId == dealId, ct)
            .ConfigureAwait(false);

        return record?.State is InsuranceState.Active or InsuranceState.InstitutionBacked;
    }
}
