using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Repositories;

public sealed class HostPaymentDetailsRepository(IdentityDbContext dbContext)
{
    public async Task<HostPaymentDetails?> GetByHostIdAsync(
        Guid hostUserId, CancellationToken cancellationToken = default) =>
        await dbContext.HostPaymentDetails
            .FirstOrDefaultAsync(h => h.HostUserId == hostUserId, cancellationToken)
            .ConfigureAwait(false);

    public void Add(HostPaymentDetails details) =>
        dbContext.HostPaymentDetails.Add(details);
}
