using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Services;

public sealed class HostStripeAccountProvider(
    IdentityDbContext dbContext) : IHostStripeAccountProvider
{
    public async Task<HostStripeAccountDto?> GetByHostUserIdAsync(
        Guid hostUserId,
        CancellationToken ct = default)
    {
        var account = await dbContext.HostStripeAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HostUserId == hostUserId, ct)
            .ConfigureAwait(false);

        if (account is null)
        {
            return null;
        }

        return new HostStripeAccountDto(
            account.HostUserId,
            account.StripeAccountId,
            account.ChargesEnabled,
            account.PayoutsEnabled);
    }
}
