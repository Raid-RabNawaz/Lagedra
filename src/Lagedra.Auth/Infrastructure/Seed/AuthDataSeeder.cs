using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lagedra.Auth.Infrastructure.Seed;

public sealed partial class AuthDataSeeder(
    UserManager<ApplicationUser> userManager,
    IClock clock,
    IOptions<SuperAdminSettings> superAdminOptions,
    ILogger<AuthDataSeeder> logger)
{
    private readonly SuperAdminSettings _superAdmin = superAdminOptions.Value;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedSuperAdminAsync(ct).ConfigureAwait(false);
        await SeedDevUsersAsync(ct).ConfigureAwait(false);
    }

    private async Task SeedSuperAdminAsync(CancellationToken ct)
    {
        _ = ct;

        if (string.IsNullOrWhiteSpace(_superAdmin.Password))
        {
            LogSuperAdminSkipped(logger);
            return;
        }

        var existing = await userManager.FindByEmailAsync(_superAdmin.Email).ConfigureAwait(false);
        if (existing is not null)
        {
            LogSuperAdminExists(logger, _superAdmin.Email);
            return;
        }

        var superAdmin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = _superAdmin.Email,
            Email = _superAdmin.Email,
            EmailConfirmed = true,
            Role = UserRole.PlatformAdmin,
            IsActive = true,
            CreatedAt = clock.UtcNow
        };

        var result = await userManager.CreateAsync(superAdmin, _superAdmin.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            LogSuperAdminFailed(logger, _superAdmin.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        LogSuperAdminCreated(logger, _superAdmin.Email);
    }

    private async Task SeedDevUsersAsync(CancellationToken ct)
    {
        _ = ct;

        // Dev accounts ship with filled-in profiles so the marketplace renders
        // real host identities out of the box (a faceless "Host" placeholder
        // makes the listing/booking flow look broken). The landlord in
        // particular gets a complete profile so its listings clear the
        // submit-for-review completeness gate without manual setup.
        var devUsers = new[]
        {
            DevUser("member@lagedra.dev", UserRole.Member, "Avery", "Collins", "Brooklyn", "NY",
                "English", "Software engineer"),
            DevUser("tenant@lagedra.dev", UserRole.Member, "Taylor", "Morgan", "Seattle", "WA",
                "English, French", "UX designer"),
            DevUser("landlord@lagedra.dev", UserRole.Member, "Jordan", "Bennett", "Austin", "TX",
                "English, Spanish", "Property manager", isHost: true),
            DevUser("arbitrator@lagedra.dev", UserRole.Arbitrator, "Riley", "Hayes", "Chicago", "IL",
                "English", "Dispute mediator"),
            DevUser("insurance@lagedra.dev", UserRole.InsurancePartner, "Casey", "Reed", "Hartford", "CT",
                "English", "Underwriting lead"),
            DevUser("institution@lagedra.dev", UserRole.InstitutionPartner, "Sydney", "Price", "Boston", "MA",
                "English", "Partnerships manager"),
        };

        foreach (var profile in devUsers)
        {
            var existing = await userManager.FindByEmailAsync(profile.Email).ConfigureAwait(false);
            if (existing is not null)
            {
                // Backfill profiles for dev accounts created before this seed
                // had names attached, but only when the host never customised
                // them — so the marketplace shows real host identities on an
                // existing database without clobbering anyone's manual edits.
                if (string.IsNullOrWhiteSpace(existing.DisplayName) &&
                    string.IsNullOrWhiteSpace(existing.FirstName))
                {
                    ApplyProfile(existing, profile);
                    await userManager.UpdateAsync(existing).ConfigureAwait(false);
                }

                continue;
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = profile.Email,
                Email = profile.Email,
                EmailConfirmed = true,
                Role = profile.Role,
                IsActive = true,
                CreatedAt = clock.UtcNow,
            };
            ApplyProfile(user, profile);

            var email = profile.Email;
            var role = profile.Role;
            var result = await userManager.CreateAsync(user, DevUserPassword).ConfigureAwait(false);
            if (result.Succeeded)
            {
#pragma warning disable CA1873 // Avoid potentially expensive logging
                LogDevUserCreated(logger, email, role.ToString());
#pragma warning restore CA1873 // Avoid potentially expensive logging
            }
            else
            {
                LogDevUserFailed(logger, email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private static void ApplyProfile(ApplicationUser user, DevUserProfile profile)
    {
        user.FirstName = profile.FirstName;
        user.LastName = profile.LastName;
        user.DisplayName = $"{profile.FirstName} {profile.LastName}";
        user.Bio = profile.Bio;
        user.ProfilePhotoUrl = profile.ProfilePhotoUrl;
        user.City = profile.City;
        user.State = profile.State;
        user.Country = "United States";
        user.Languages = profile.Languages;
        user.Occupation = profile.Occupation;
        user.DateOfBirth = profile.DateOfBirth;
        user.IsGovernmentIdVerified = profile.IsHost;
        user.IsPhoneVerified = profile.IsHost;
        user.ResponseRatePercent = profile.IsHost ? 98 : null;
        user.ResponseTimeMinutes = profile.IsHost ? 30 : null;
    }

    private const string DevUserPassword = "Dev@1234!";

    private static DevUserProfile DevUser(
        string email,
        UserRole role,
        string firstName,
        string lastName,
        string city,
        string state,
        string languages,
        string occupation,
        bool isHost = false)
    {
        var bio = isHost
            ? $"Hi, I'm {firstName}. I manage a handful of well-kept homes and care about smooth, " +
              "honest stays. Reach out any time — I usually reply within the hour."
            : $"Hi, I'm {firstName}, based in {city}. Looking forward to a great stay.";

        return new DevUserProfile(
            email,
            role,
            firstName,
            lastName,
            bio,
            new Uri($"https://api.dicebear.com/7.x/initials/svg?seed={Uri.EscapeDataString($"{firstName} {lastName}")}"),
            city,
            state,
            languages,
            occupation,
            new DateOnly(1990, 5, 14),
            isHost);
    }

    private sealed record DevUserProfile(
        string Email,
        UserRole Role,
        string FirstName,
        string LastName,
        string Bio,
        Uri ProfilePhotoUrl,
        string City,
        string State,
        string Languages,
        string Occupation,
        DateOnly DateOfBirth,
        bool IsHost);

    [LoggerMessage(Level = LogLevel.Warning, Message = "SuperAdmin seed skipped: Seed:SuperAdmin:Password is not configured.")]
    private static partial void LogSuperAdminSkipped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "SuperAdmin already exists: {Email}")]
    private static partial void LogSuperAdminExists(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Information, Message = "SuperAdmin created: {Email}")]
    private static partial void LogSuperAdminCreated(ILogger logger, string email);

    [LoggerMessage(Level = LogLevel.Error, Message = "SuperAdmin creation failed for {Email}: {Errors}")]
    private static partial void LogSuperAdminFailed(ILogger logger, string email, string errors);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dev user created: {Email} ({Role})")]
    private static partial void LogDevUserCreated(ILogger logger, string email, string role);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dev user creation failed for {Email}: {Errors}")]
    private static partial void LogDevUserFailed(ILogger logger, string email, string errors);
}
