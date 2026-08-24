using Lagedra.Auth.Domain;
using Lagedra.Auth.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class UserLookupService(AuthDbContext dbContext) : IUserLookupService
{
    public async Task<Guid?> FindUserIdByEmailAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToUpperInvariant();
        return await dbContext.Users.AsNoTracking()
            .Where(u => u.NormalizedEmail == normalized && !u.IsDeleted)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
    }

    public async Task<UserAccountLookupDto?> FindAccountByEmailAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var normalized = email.Trim().ToUpperInvariant();
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.NormalizedEmail == normalized && u.IsActive && !u.IsDeleted,
                ct)
            .ConfigureAwait(false);

        return user is null ? null : Map(user);
    }

    public async Task<UserAccountLookupDto?> FindAccountByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive && !u.IsDeleted, ct)
            .ConfigureAwait(false);

        return user is null ? null : Map(user);
    }

    private static UserAccountLookupDto Map(ApplicationUser user)
    {
        var combined = $"{user.FirstName} {user.LastName}".Trim();
        var displayName = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName!
            : !string.IsNullOrWhiteSpace(combined)
                ? combined
                : user.Email ?? "Lagedra member";

        return new UserAccountLookupDto(user.Id, displayName, user.Email ?? string.Empty);
    }
}
