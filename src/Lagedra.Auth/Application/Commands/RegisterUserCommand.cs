using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Emails;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Sms;
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
            PhoneNumber = NormalizePhone(request.Phone),
            City = Trim(request.City),
            SignupType = Trim(request.SignupType),
            PortfolioSize = Trim(request.PortfolioSize),
            HousingType = Trim(request.HousingType),
            PlacementsPerYear = Trim(request.PlacementsPerYear),
            IsPreLaunchSignup = preLaunch
        };

        // Founding hosts get a real account (listings + Hostaway import) during
        // pre-launch: password-less signup, verify email, then set a password.
        // Institution partners stay on the password-less waitlist.
        if (preLaunch)
        {
            return PreLaunchAccess.IsHostSignup(user.SignupType)
                ? await CreateFoundingHostAccountAsync(user, cancellationToken).ConfigureAwait(true)
                : await CreatePreLaunchLeadAsync(user, cancellationToken).ConfigureAwait(true);
        }

        return await CreateStandardAccountAsync(user, request.Password, cancellationToken)
            .ConfigureAwait(true);
    }

    /// <summary>
    /// Pre-launch waitlist (partners): create a password-less, inactive account
    /// so the email is reserved and the lead is captured. The account cannot
    /// log in until launch.
    /// </summary>
    private async Task<Result<RegisterResultDto>> CreatePreLaunchLeadAsync(
        ApplicationUser user, CancellationToken ct)
    {
        var identityResult = await userManager.CreateAsync(user).ConfigureAwait(true);
        if (!identityResult.Succeeded)
        {
            return AuthErrors.IdentityError(identityResult.Errors.First().Description);
        }

        await SendPreLaunchPartnerEmailAsync(user, ct).ConfigureAwait(true);

        return Result<RegisterResultDto>.Success(
            new RegisterResultDto(user.Id, VerificationUrl: null, VerificationToken: null, IsPreLaunch: true));
    }

    /// <summary>
    /// Pre-launch founding host: password-less account + verification email.
    /// The SPA verify page chains into "set your password" (the verify
    /// endpoint hands back a password-setup token), after which the host can
    /// sign in to the limited listings + Hostaway surface.
    /// </summary>
    private async Task<Result<RegisterResultDto>> CreateFoundingHostAccountAsync(
        ApplicationUser user, CancellationToken ct)
    {
        var identityResult = await userManager.CreateAsync(user).ConfigureAwait(true);
        if (!identityResult.Succeeded)
        {
            return AuthErrors.IdentityError(identityResult.Errors.First().Description);
        }

        var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(true);
        var encodedToken = Uri.EscapeDataString(rawToken);
        var frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var verifyUrl = WelcomeEmailComposer.BuildSpaVerifyUrl(frontendUrl, user.Id, encodedToken);
        var email = WelcomeEmailComposer.BuildFoundingHostWelcomeEmail(user, verifyUrl);

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = email.Subject,
            HtmlBody = email.HtmlBody,
            PlainTextBody = email.PlainTextBody
        }, ct).ConfigureAwait(true);

        return Result<RegisterResultDto>.Success(
            new RegisterResultDto(user.Id, verifyUrl, rawToken, IsPreLaunch: false));
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
        var frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var verifyUrl = WelcomeEmailComposer.BuildSpaVerifyUrl(frontendUrl, user.Id, encodedToken);
        var email = WelcomeEmailComposer.BuildWelcomeEmail(user, verifyUrl);

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = email.Subject,
            HtmlBody = email.HtmlBody,
            PlainTextBody = email.PlainTextBody
        }, ct).ConfigureAwait(true);

        return Result<RegisterResultDto>.Success(
            new RegisterResultDto(user.Id, verifyUrl, rawToken, IsPreLaunch: false));
    }

    private async Task SendPreLaunchPartnerEmailAsync(ApplicationUser user, CancellationToken ct)
    {
        var frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var howItWorksUrl = WelcomeEmailComposer.BuildHowItWorksUrl(frontendUrl);
        var email = WelcomeEmailComposer.BuildPreLaunchPartnerEmail(user, howItWorksUrl);

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = email.Subject,
            HtmlBody = email.HtmlBody,
            PlainTextBody = email.PlainTextBody
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

    /// <summary>
    /// Best-effort E.164 normalization: signup phone is a lead-capture field,
    /// so registration is never rejected over its format — un-normalizable
    /// input is stored trimmed and enforced later at profile save /
    /// verification time.
    /// </summary>
    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        return PhoneNumberE164.TryNormalize(phone, out var normalized) ? normalized : phone.Trim();
    }
}
