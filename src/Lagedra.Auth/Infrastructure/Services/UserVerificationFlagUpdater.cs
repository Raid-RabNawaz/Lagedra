using Lagedra.Auth.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class UserVerificationFlagUpdater(AuthDbContext dbContext) : IUserVerificationFlagUpdater
{
    public async Task MarkGovernmentIdVerifiedAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            return;
        }

        user.IsGovernmentIdVerified = true;
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
