using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Integration;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class UserPhoneResolver(UserManager<ApplicationUser> userManager) : IUserPhoneResolver
{
    public async Task<string?> GetPhoneAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(user?.PhoneNumber) ? null : user.PhoneNumber;
    }

    public async Task<bool> IsPhoneVerifiedAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        return user is { IsPhoneVerified: true }
            && !string.IsNullOrWhiteSpace(user.PhoneNumber);
    }
}
