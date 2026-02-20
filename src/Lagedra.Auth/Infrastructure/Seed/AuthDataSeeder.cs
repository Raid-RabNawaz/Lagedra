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

        var devUsers = new[]
        {
            ("tenant@lagedra.dev",   "Dev@1234!",  UserRole.Tenant),
            ("landlord@lagedra.dev", "Dev@1234!",  UserRole.Landlord),
            ("arbitrator@lagedra.dev", "Dev@1234!", UserRole.Arbitrator),
            ("insurance@lagedra.dev", "Dev@1234!",  UserRole.InsurancePartner),
            ("institution@lagedra.dev", "Dev@1234!", UserRole.InstitutionPartner),
        };

        foreach (var (email, password, role) in devUsers)
        {
            var existing = await userManager.FindByEmailAsync(email).ConfigureAwait(false);
            if (existing is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Role = role,
                IsActive = true,
                CreatedAt = clock.UtcNow
            };

            var result = await userManager.CreateAsync(user, password).ConfigureAwait(false);
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
