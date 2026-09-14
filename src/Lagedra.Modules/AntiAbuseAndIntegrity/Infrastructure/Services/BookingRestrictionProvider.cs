using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Services;

public sealed class BookingRestrictionProvider(IntegrityDbContext dbContext) : IBookingRestrictionProvider
{
    public async Task<bool> IsBlockedFromBookingAsync(Guid userId, CancellationToken ct = default)
    {
        return await dbContext.AccountRestrictions
            .AsNoTracking()
            .AnyAsync(
                r => r.UserId == userId
                     && (r.RestrictionLevel == RestrictionLevel.Suspended
                         || r.RestrictionLevel == RestrictionLevel.Banned),
                ct)
            .ConfigureAwait(false);
    }
}
