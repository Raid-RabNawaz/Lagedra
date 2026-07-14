using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Auth.Application.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string? Password,
    UserRole Role,
    string? FullName = null,
    string? CompanyName = null,
    string? Phone = null,
    string? City = null,
    string? SignupType = null,
    string? PortfolioSize = null,
    string? HousingType = null,
    string? PlacementsPerYear = null) : IRequest<Result<RegisterResultDto>>;

public sealed class RegisterUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IClock clock,
    IConfiguration configuration,
    IPlatformSettingsService platformSettings)
    : IRequestHandler<RegisterUserCommand, Result<RegisterResultDto>>
{
    public async Task<Result<RegisterResultDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(true);
        if (existing is not null)
        {
            return AuthErrors.EmailAlreadyExists;
        }

        if (request.Role is not (UserRole.Member or UserRole.InstitutionPartner))
        {
            return AuthErrors.IdentityError("Self-registration is only available for Member and InstitutionPartner roles.");
        }

        var preLaunch = await platformSettings
            .GetBoolAsync(PlatformSettingKeys.PreLaunchEnabled, defaultValue: false, cancellationToken)
            .ConfigureAwait(true);

        var (firstName, lastName) = SplitFullName(request.FullName);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            Role = request.Role,
            IsActive = false,
            CreatedAt = clock.UtcNow,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim(),
            CompanyName = Trim(request.CompanyName),
            PhoneNumber = Trim(request.Phone),
            City = Trim(request.City),
            SignupType = Trim(request.SignupType),
            PortfolioSize = Trim(request.PortfolioSize),
            HousingType = Trim(request.HousingType),
            PlacementsPerYear = Trim(request.PlacementsPerYear),
            IsPreLaunchSignup = preLaunch
        };

        return preLaunch
            ? await CreatePreLaunchLeadAsync(user, cancellationToken).ConfigureAwait(true)
            : await CreateStandardAccountAsync(user, request.Password, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// Pre-launch waitlist: create a password-less, inactive account (so the
    /// email is reserved and the lead is captured) and send the founding-partner
    /// email instead of the usual verify/welcome email. The account cannot log
    /// in until launch, when an admin turns the flag off and invites members to
    /// set a password.
    /// </summary>
    private async Task<Result<RegisterResultDto>> CreatePreLaunchLeadAsync(
        ApplicationUser user, CancellationToken ct)
    {
        var identityResult = await userManager.CreateAsync(user).ConfigureAwait(true);
        if (!identityResult.Succeeded)
        {
            return AuthErrors.IdentityError(identityResult.Errors.First().Description);
        }

        await SendPreLaunchEmailAsync(user, ct).ConfigureAwait(true);

        return Result<RegisterResultDto>.Success(
            new RegisterResultDto(user.Id, VerificationUrl: null, VerificationToken: null, IsPreLaunch: true));
    }

    /// <summary>
    /// Normal sign-up (pre-launch off): password required, standard email
    /// verification, and the account becomes loginable once verified.
    /// </summary>
    private async Task<Result<RegisterResultDto>> CreateStandardAccountAsync(
        ApplicationUser user, string? password, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return AuthErrors.PasswordRequired;
        }

        var identityResult = await userManager.CreateAsync(user, password).ConfigureAwait(true);
        if (!identityResult.Succeeded)
        {
            return AuthErrors.IdentityError(identityResult.Errors.First().Description);
        }

        var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(true);
        var encodedToken = Uri.EscapeDataString(rawToken);
        var baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5000";
        var verifyUrl = $"{baseUrl}/v1/auth/verify-email?userId={user.Id}&token={encodedToken}";

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "Verify your Lagedra account",
            HtmlBody = $"""
                <h2>Welcome to Lagedra</h2>
                <p>Click the link below to verify your email address and activate your account.</p>
                <p><a href="{verifyUrl}">Verify Email</a></p>
                <p>This link expires in 24 hours.</p>
                """,
            PlainTextBody = $"Verify your email: {verifyUrl}"
        }, ct).ConfigureAwait(true);

        return Result<RegisterResultDto>.Success(
            new RegisterResultDto(user.Id, new Uri(verifyUrl), rawToken, IsPreLaunch: false));
    }

    private async Task SendPreLaunchEmailAsync(ApplicationUser user, CancellationToken ct)
    {
        var isPartner = string.Equals(user.SignupType, "Partner", StringComparison.OrdinalIgnoreCase);
        var frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var howItWorksUrl = $"{frontendUrl}/how-it-works";

        var intro = isPartner
            ? "Thanks for joining our founding partners. We're putting the final pieces in place ahead of launch — and you're on the early list."
            : "Thanks for joining as a founding host. We're putting the final pieces in place ahead of launch — and you're on the early list.";

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "You're in — welcome to Lagedra",
            HtmlBody = $"""
                <!doctype html>
                <html>
                <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #1A1A2E;">
                  <h2 style="margin-top: 32px;">You're in. Welcome to Lagedra.</h2>
                  <p>{intro}</p>
                  <h3 style="margin-top: 28px;">What happens next</h3>
                  <ol style="line-height: 1.6;">
                    <li><strong>We'll reach out personally.</strong> Someone from our team will contact you soon to understand your needs and get you set up.</li>
                    <li><strong>We'll connect you to inventory.</strong> We'll show you how to search and request verified homes across the markets you cover.</li>
                    <li><strong>You move faster.</strong> Vetted 30+ day housing, ready when you need it — without the usual scramble.</li>
                  </ol>
                  <p>As a founding partner, you're first in line — and there's no cost to join. We'll be in touch shortly. In the meantime, keep an eye on your inbox.</p>
                  <p style="margin: 24px 0;">
                    <a href="{howItWorksUrl}"
                       style="display: inline-block; background: #5B3FE0; color: #fff; padding: 12px 20px; border-radius: 10px; text-decoration: none; font-weight: 600;">
                      Explore how it works
                    </a>
                  </p>
                  <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
                  <p style="font-size: 12px; color: #999;">
                    You're receiving this because you joined the Lagedra founding-partner list.
                  </p>
                </body>
                </html>
                """,
            PlainTextBody = $"""
                You're in. Welcome to Lagedra.

                {intro}

                What happens next
                1. We'll reach out personally.
                2. We'll connect you to inventory.
                3. You move faster.

                As a founding partner, you're first in line — and there's no cost to join.

                Explore how it works: {howItWorksUrl}
                """
        }, ct).ConfigureAwait(true);
    }

    private static (string? First, string? Last) SplitFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return (null, null);
        }

        var trimmed = fullName.Trim();
        var spaceIndex = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return spaceIndex < 0
            ? (trimmed, null)
            : (trimmed[..spaceIndex], trimmed[(spaceIndex + 1)..].Trim());
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
