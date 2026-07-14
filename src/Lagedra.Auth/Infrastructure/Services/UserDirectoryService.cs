using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Integration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Auth.Infrastructure.Services;

public sealed class UserDirectoryService(UserManager<ApplicationUser> userManager)
    : IUserDirectoryService
{
    public async Task<IReadOnlyDictionary<Guid, UserDirectoryEntry>> GetEntriesAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, UserDirectoryEntry>();
        }

        var users = await userManager.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                u.FirstName,
                u.LastName,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return users.ToDictionary(
            u => u.Id,
            u => new UserDirectoryEntry(
                u.Id,
                u.Email ?? string.Empty,
                ResolveDisplayName(u.DisplayName, u.FirstName, u.LastName, u.Email)));
    }

    private static string ResolveDisplayName(
        string? displayName,
        string? firstName,
        string? lastName,
        string? email)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        var combined = $"{firstName} {lastName}".Trim();
        if (!string.IsNullOrWhiteSpace(combined))
        {
            return combined;
        }

        return string.IsNullOrWhiteSpace(email) ? "Member" : email.Trim();
    }
}
