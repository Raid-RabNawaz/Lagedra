using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Services;

public sealed record ResolvedUserIdentity(string DisplayName, string Email);

/// <summary>
/// Resolves display names and emails for platform users, preferring the user
/// directory and falling back to guest-invite audit data recorded by the
/// organization (useful when the directory entry is sparse).
/// </summary>
internal static class PartnerUserIdentityResolver
{
    public static async Task<IReadOnlyDictionary<Guid, ResolvedUserIdentity>> ResolveAsync(
        PartnerDbContext dbContext,
        IUserDirectoryService userDirectory,
        Guid organizationId,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, ResolvedUserIdentity>();
        }

        var distinctIds = userIds.Distinct().ToList();

        var directory = await userDirectory
            .GetEntriesAsync(distinctIds, cancellationToken)
            .ConfigureAwait(false);

        var invites = await dbContext.GuestInvites
            .AsNoTracking()
            .Where(i => i.OrganizationId == organizationId
                     && distinctIds.Contains(i.InvitedUserId))
            .OrderByDescending(i => i.InvitedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var latestInviteByUser = invites
            .GroupBy(i => i.InvitedUserId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new Dictionary<Guid, ResolvedUserIdentity>(distinctIds.Count);
        foreach (var userId in distinctIds)
        {
            directory.TryGetValue(userId, out var entry);
            latestInviteByUser.TryGetValue(userId, out var invite);

            var email = !string.IsNullOrWhiteSpace(entry?.Email)
                ? entry!.Email
                : invite?.Email ?? string.Empty;

            // Prefer a directory display name that isn't just the email; then
            // the invite's full name; then the email. When nothing resolves
            // (e.g. the account no longer exists) leave the name empty so the
            // UI falls back to the user id instead of showing a made-up label.
            var displayName = !string.IsNullOrWhiteSpace(entry?.DisplayName)
                && entry!.DisplayName != email
                ? entry.DisplayName
                : !string.IsNullOrWhiteSpace(invite?.FullName)
                    ? invite!.FullName
                    : !string.IsNullOrWhiteSpace(entry?.DisplayName)
                        ? entry!.DisplayName
                        : !string.IsNullOrWhiteSpace(email)
                            ? email
                            : string.Empty;

            result[userId] = new ResolvedUserIdentity(displayName, email);
        }

        return result;
    }
}
