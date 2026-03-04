using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.InsuranceIntegration.Infrastructure.Services;

public sealed class UserInsuranceStatusProvider(InsuranceDbContext dbContext) : IUserInsuranceStatusProvider
{
    public async Task<UserInsuranceStatusDto> GetBestStatusForUserAsync(
        Guid userId, CancellationToken ct = default)
    {
        var hasActive = await dbContext.PolicyRecords
            .AsNoTracking()
            .AnyAsync(r => r.TenantUserId == userId
                && (r.State == InsuranceState.Active || r.State == InsuranceState.InstitutionBacked), ct)
            .ConfigureAwait(false);

        var hasInstitutionBacked = hasActive && await dbContext.PolicyRecords
            .AsNoTracking()
            .AnyAsync(r => r.TenantUserId == userId
                && r.State == InsuranceState.InstitutionBacked, ct)
            .ConfigureAwait(false);

        return new UserInsuranceStatusDto(hasActive, hasInstitutionBacked);
    }

    public async Task<Guid?> GetTenantUserIdForDealAsync(Guid dealId, CancellationToken ct = default)
    {
        var record = await dbContext.PolicyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.DealId == dealId, ct)
            .ConfigureAwait(false);

        return record?.TenantUserId;
    }
}
