using Lagedra.Modules.Privacy.Domain.Enums;
using Lagedra.Modules.Privacy.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Privacy.Infrastructure.Services;

public sealed class ConsentChecker(PrivacyDbContext dbContext) : IConsentChecker
{
    private static readonly ConsentType[] RequiredConsents = [ConsentType.KYCConsent, ConsentType.DataProcessing];

    public async Task<bool> HasRequiredConsentsAsync(Guid userId, CancellationToken ct = default)
    {
        var userConsent = await dbContext.UserConsents
            .AsNoTracking()
            .Include(uc => uc.ConsentRecords)
            .FirstOrDefaultAsync(uc => uc.UserId == userId, ct)
            .ConfigureAwait(false);

        if (userConsent is null)
        {
            return false;
        }

        foreach (var required in RequiredConsents)
        {
            var record = userConsent.ConsentRecords
                .FirstOrDefault(r => r.ConsentType == required && r.WithdrawnAt == null);
            if (record is null)
            {
                return false;
            }
        }

        return true;
    }
}
