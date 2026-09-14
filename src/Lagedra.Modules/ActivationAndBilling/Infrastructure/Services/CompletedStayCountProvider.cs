using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;

public sealed class CompletedStayCountProvider(BillingDbContext dbContext) : ICompletedStayCountProvider
{
    public async Task<int> GetClosedStayCountAsync(Guid tenantUserId, CancellationToken ct = default)
    {
        return await dbContext.BillingAccounts
            .AsNoTracking()
            .CountAsync(
                a => a.TenantUserId == tenantUserId && a.Status == BillingAccountStatus.Closed,
                ct)
            .ConfigureAwait(false);
    }
}
