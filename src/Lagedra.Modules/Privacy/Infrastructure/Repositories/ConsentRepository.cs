using Lagedra.Modules.Privacy.Domain.Aggregates;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Infrastructure.Repositories;

public sealed class ConsentRepository(PrivacyDbContext dbContext)
{
    public async Task<UserConsent?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.UserConsents
            .Include(uc => uc.ConsentRecords)
            .FirstOrDefaultAsync(uc => uc.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

    public void Add(UserConsent userConsent) =>
        dbContext.UserConsents.Add(userConsent);
}
