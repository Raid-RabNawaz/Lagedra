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
        var status = await GetRequiredConsentStatusAsync(userId, ct).ConfigureAwait(false);
        return status.HasRequired;
    }

    public async Task<ConsentStatus> GetRequiredConsentStatusAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var userConsent = await dbContext.UserConsents
            .AsNoTracking()
            .Include(uc => uc.ConsentRecords)
            .FirstOrDefaultAsync(uc => uc.UserId == userId, ct)
            .ConfigureAwait(false);

        if (userConsent is null)
        {
            return new ConsentStatus(
                HasRequired: false,
                MissingConsentTypes: RequiredConsents.Select(c => c.ToString()).ToArray());
        }

        var missing = new List<string>(RequiredConsents.Length);
        foreach (var required in RequiredConsents)
        {
            var record = userConsent.ConsentRecords
                .FirstOrDefault(r => r.ConsentType == required && r.WithdrawnAt == null);
            if (record is null)
            {
                missing.Add(required.ToString());
            }
        }

        return new ConsentStatus(missing.Count == 0, missing);
    }
}
