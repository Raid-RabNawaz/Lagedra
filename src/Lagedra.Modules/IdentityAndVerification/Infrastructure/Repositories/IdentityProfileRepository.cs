using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Repositories;

public sealed class IdentityProfileRepository(IdentityDbContext dbContext)
{
    public async Task<IdentityProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.IdentityProfiles
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<IdentityProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.IdentityProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

    public void Add(IdentityProfile profile) =>
        dbContext.IdentityProfiles.Add(profile);
}
