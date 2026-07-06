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

        var displayName = ResolveDisplayName(user);

        return new HostProfileDto(
            displayName,
            user.ProfilePhotoUrl,
            user.IsGovernmentIdVerified,
            user.IsPhoneVerified,
            user.ResponseRatePercent,
            user.ResponseTimeMinutes,
            user.CreatedAt);
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
