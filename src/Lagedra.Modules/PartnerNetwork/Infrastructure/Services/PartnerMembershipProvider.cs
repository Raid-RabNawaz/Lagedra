using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Infrastructure.Services;

public sealed class PartnerMembershipProvider(PartnerDbContext dbContext)
    : IPartnerMembershipProvider
{
    public async Task<Guid?> GetPartnerOrganizationIdAsync(
        Guid userId, CancellationToken ct = default)
    {
        var member = await dbContext.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId, ct)
            .ConfigureAwait(false);

        return member?.OrganizationId;
    }
}
