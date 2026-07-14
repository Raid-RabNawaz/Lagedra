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

    public async Task<IReadOnlyList<Guid>> GetMemberUserIdsAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        return await dbContext.Members
            .AsNoTracking()
            .Where(m => m.OrganizationId == organizationId)
            .Select(m => m.UserId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<string?> GetOrganizationNameAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        return await dbContext.Organizations
            .AsNoTracking()
            .Where(o => o.Id == organizationId)
            .Select(o => o.Name)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }
}
