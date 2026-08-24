using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Integration;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class UserEmailResolver(UserManager<ApplicationUser> userManager) : IUserEmailResolver
{
    public async Task<string?> GetEmailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        return user?.Email;
    }

    public async Task<Guid?> GetUserIdByEmailAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var user = await userManager.FindByEmailAsync(email.Trim()).ConfigureAwait(false);
        return user?.Id;
    }
}
