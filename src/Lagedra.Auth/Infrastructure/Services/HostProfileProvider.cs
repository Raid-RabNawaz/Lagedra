using Lagedra.Auth.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class HostProfileProvider(AuthDbContext dbContext) : IHostProfileProvider
{
    public async Task<HostProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            return null;
        }

        var displayName = user.DisplayName
            ?? $"{user.FirstName} {user.LastName}".Trim();

        return new HostProfileDto(
            string.IsNullOrWhiteSpace(displayName) ? null : displayName,
            user.ProfilePhotoUrl,
            user.IsGovernmentIdVerified,
            user.IsPhoneVerified,
            user.ResponseRatePercent,
            user.ResponseTimeMinutes,
            user.CreatedAt);
    }
}
