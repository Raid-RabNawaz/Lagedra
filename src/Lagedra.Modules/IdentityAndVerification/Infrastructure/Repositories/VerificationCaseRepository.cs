using Lagedra.Modules.IdentityAndVerification.Domain.Aggregates;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Repositories;

public sealed class VerificationCaseRepository(IdentityDbContext dbContext)
{
    public async Task<VerificationCase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await dbContext.VerificationCases
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);

    public async Task<VerificationCase?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await dbContext.VerificationCases
            .Where(c => c.UserId == userId && c.CompletedAt == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    public void Add(VerificationCase verificationCase) =>
        dbContext.VerificationCases.Add(verificationCase);
}
