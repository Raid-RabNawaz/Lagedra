using Lagedra.Compliance.Domain;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Compliance.Infrastructure.Services;

public sealed class UserViolationCountProvider(ComplianceDbContext dbContext) : IUserViolationCountProvider
{
    public async Task<int> GetActiveViolationCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.Violations
            .AsNoTracking()
            .CountAsync(
                v => v.TargetUserId == userId
                     && v.Status != ViolationStatus.Resolved
                     && v.Status != ViolationStatus.Dismissed,
                ct)
            .ConfigureAwait(false);
    }
}
