using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Services;

public sealed class PartnerOrganizationBillingProfile(PartnerDbContext dbContext)
    : IPartnerOrganizationBillingProfile
{
    public async Task<string?> GetNameAsync(Guid organizationId, CancellationToken ct = default)
    {
        return await dbContext.Organizations
            .AsNoTracking()
            .Where(o => o.Id == organizationId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<string?> GetStripeCustomerIdAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        return await dbContext.Organizations
            .AsNoTracking()
            .Where(o => o.Id == organizationId)
            .Select(o => o.StripeCustomerId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task SetStripeCustomerIdAsync(
        Guid organizationId,
        string stripeCustomerId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeCustomerId);

        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            .ConfigureAwait(false);

        if (org is null)
        {
            throw new InvalidOperationException($"Partner organization {organizationId} was not found.");
        }

        org.SetStripeCustomerId(stripeCustomerId);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
