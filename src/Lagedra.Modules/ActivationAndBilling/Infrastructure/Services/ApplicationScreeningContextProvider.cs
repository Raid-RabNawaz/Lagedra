using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;

public sealed class ApplicationScreeningContextProvider(BillingDbContext dbContext)
    : IApplicationScreeningContextProvider
{
    public async Task<ApplicationScreeningContextDto?> GetAsync(
        Guid applicationId,
        CancellationToken ct = default)
    {
        var app = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            .ConfigureAwait(false);

        if (app is null)
        {
            return null;
        }

        return new ApplicationScreeningContextDto(
            app.Id,
            app.TenantUserId,
            app.LandlordUserId,
            app.HomeOwnerUserId,
            app.ListingId,
            app.Status.ToString());
    }
}
