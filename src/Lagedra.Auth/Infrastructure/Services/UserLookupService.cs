using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Integration;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class UserLookupService(UserManager<ApplicationUser> userManager)
    : IUserLookupService
{
    public async Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var user = await userManager.FindByEmailAsync(email.Trim()).ConfigureAwait(false);
        return user?.Id;
    }
}
