using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Entities;
using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Enums;
using Lagedra.Modules.AntiAbuseAndIntegrity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.Services;

internal static class FraudFlagWriter
{
    public static async Task RaiseIfMissingAsync(
        IntegrityDbContext dbContext,
        Guid userId,
        FraudFlagType flagType,
        Severity severity,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.FraudFlags
            .AnyAsync(
                f => f.UserId == userId && f.FlagType == flagType,
                cancellationToken)
            .ConfigureAwait(false);

        if (exists)
        {
            return;
        }

        dbContext.FraudFlags.Add(FraudFlag.Create(userId, flagType, severity));
    }
}
