using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Services;

public sealed class VerificationSignalProvider(IdentityDbContext dbContext) : IVerificationSignalProvider
{
    public async Task<VerificationSignalDto?> GetSignalsAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await dbContext.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return null;
        }

        var latestBgCheck = await dbContext.BackgroundCheckReports
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReceivedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return new VerificationSignalDto(
            IsIdentityVerified: profile.Status == VerificationStatus.Verified,
            IsIdentityPending: profile.Status == VerificationStatus.Pending,
            IsIdentityFailed: profile.Status == VerificationStatus.Failed,
            IsBackgroundCheckPassed: latestBgCheck?.Result == BackgroundCheckResult.Pass,
            IsBackgroundCheckFailed: latestBgCheck?.Result == BackgroundCheckResult.Fail,
            IsBackgroundCheckUnderReview: latestBgCheck?.Result == BackgroundCheckResult.Review);
    }
}
