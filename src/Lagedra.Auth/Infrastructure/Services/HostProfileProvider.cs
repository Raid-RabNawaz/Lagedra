using Lagedra.Auth.Domain;
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

        return ToProfile(user);
    }

    public async Task<HostProfileCompletenessDto> GetProfileCompletenessAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            .ConfigureAwait(false);

        return ComputeCompleteness(user);
    }

    public async Task<IReadOnlyDictionary<Guid, HostReviewSnapshot>> GetReviewSnapshotsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, HostReviewSnapshot>();
        }

        var users = await dbContext.Users
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byId = users.ToDictionary(u => u.Id);
        var snapshots = new Dictionary<Guid, HostReviewSnapshot>(ids.Count);
        foreach (var id in ids)
        {
            byId.TryGetValue(id, out var user);
            snapshots[id] = new HostReviewSnapshot(
                user is null ? null : ToProfile(user),
                ComputeCompleteness(user));
        }

        return snapshots;
    }

    private static HostProfileDto ToProfile(ApplicationUser user) =>
        new(
            ResolveDisplayName(user),
            user.ProfilePhotoUrl,
            user.IsGovernmentIdVerified,
            user.IsPhoneVerified,
            user.ResponseRatePercent,
            user.ResponseTimeMinutes,
            user.CreatedAt);

    private static string? ResolveDisplayName(ApplicationUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            return user.DisplayName;
        }

        var combined = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(combined) ? null : combined;
    }

    // The profile-completeness signals below are kept deliberately in sync with
    // the web client (apps/web/src/features/auth/lib/profileCompleteness.ts) so
    // the host sees the same percentage the server enforces. Each signal is
    // weighted equally; the percentage is filled / total rounded to a whole
    // number. Verification badges (gov-ID / phone) are intentionally excluded —
    // they depend on external checks and shouldn't block a host from listing.
    private static HostProfileCompletenessDto ComputeCompleteness(ApplicationUser? user)
    {
        if (user is null)
        {
            return new HostProfileCompletenessDto(0, Array.Empty<string>());
        }

        var hasName =
            !string.IsNullOrWhiteSpace(user.DisplayName) ||
            (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName));

        var signals = new (string Label, bool Filled)[]
        {
            ("Name", hasName),
            ("Profile photo", user.ProfilePhotoUrl is not null),
            ("Bio", !string.IsNullOrWhiteSpace(user.Bio)),
            ("City", !string.IsNullOrWhiteSpace(user.City)),
            ("Country", !string.IsNullOrWhiteSpace(user.Country)),
            ("Languages", !string.IsNullOrWhiteSpace(user.Languages)),
            ("Occupation", !string.IsNullOrWhiteSpace(user.Occupation)),
            ("Date of birth", user.DateOfBirth is not null),
        };

        var filled = signals.Count(s => s.Filled);
        var percent = (int)Math.Round(filled * 100.0 / signals.Length, MidpointRounding.AwayFromZero);
        var missing = signals.Where(s => !s.Filled).Select(s => s.Label).ToList();

        return new HostProfileCompletenessDto(percent, missing);
    }
}
