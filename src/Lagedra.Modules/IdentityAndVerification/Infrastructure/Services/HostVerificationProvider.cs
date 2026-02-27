using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Infrastructure.Services;

public sealed class HostVerificationProvider(IdentityDbContext dbContext) : IHostVerificationProvider
{
    public async Task<HostVerificationDto?> GetVerificationAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await dbContext.IdentityProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            .ConfigureAwait(false);

        if (profile is null)
        {
            return null;
        }

        var isVerified = profile.Status == VerificationStatus.Verified;
        var isKycComplete = profile.Status == VerificationStatus.Verified;

        return new HostVerificationDto(isVerified, isKycComplete);
    }
}
