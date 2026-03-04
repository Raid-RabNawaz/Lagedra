using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;

public sealed class DealApplicationStatusProvider(BillingDbContext dbContext) : IDealApplicationStatusProvider
{
    public async Task<bool> IsApprovedAsync(Guid dealId, CancellationToken ct = default)
    {
        return await dbContext.DealApplications
            .AsNoTracking()
            .AnyAsync(a => a.DealId == dealId && a.Status == DealApplicationStatus.Approved, ct)
            .ConfigureAwait(false);
    }

    public async Task<DealParticipantsDto?> GetParticipantsAsync(Guid dealId, CancellationToken ct = default)
    {
        var app = await dbContext.DealApplications
            .AsNoTracking()
            .Where(a => a.DealId == dealId)
            .Select(a => new { a.LandlordUserId, a.TenantUserId })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return app is null ? null : new DealParticipantsDto(app.LandlordUserId, app.TenantUserId);
    }
}
