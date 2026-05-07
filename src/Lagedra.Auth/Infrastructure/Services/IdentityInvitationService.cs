using System.Security.Cryptography;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lagedra.Auth.Infrastructure.Services;

/// <summary>
/// Implements <see cref="IIdentityInvitationService"/> for partner-driven guest provisioning.
/// See the interface XML doc for the full contract.
/// </summary>
public sealed partial class IdentityInvitationService(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IClock clock,
    IConfiguration configuration,
    ILogger<IdentityInvitationService> logger)
    : IIdentityInvitationService
{
    /// <summary>Set-password tokens expire 7 days after issuance.</summary>
    public static readonly TimeSpan SetPasswordTokenLifetime = TimeSpan.FromDays(7);

    public async Task<Result<InvitedUserDto>> CreateOrFindInvitedUserAsync(
        InvitedUserRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Email);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.InvitingOrganizationName);

        var existing = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (existing is not null)
        {
            // Idempotent: surface the existing user id so the caller can attach the
            // endorsement / direct reservation to the right account without re-inviting.
            LogExistingUserFound(logger, request.Email);
            return Result<InvitedUserDto>.Success(new InvitedUserDto(
                existing.Id,
                existing.Email!,
                WasJustCreated: false,
                SetPasswordUrl: null,
                TokenExpiresAt: null));
        }

        var (firstName, lastName) = SplitName(request.FullName);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            Role = UserRole.Member,
            IsActive = true,
            CreatedAt = clock.UtcNow,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = request.FullName.Trim()
        };

        var randomPassword = GenerateRandomPassword();
        var createResult = await userManager.CreateAsync(user, randomPassword).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            return Result<InvitedUserDto>.Failure(
                AuthErrors.IdentityError(createResult.Errors.First().Description));
        }

        var rawToken = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        var encoded = Uri.EscapeDataString(rawToken);
        var baseUrl = configuration["App:FrontendUrl"] ?? "http://localhost:3000";
        var setPasswordUrl = new Uri(
            $"{baseUrl}/auth/set-password?userId={user.Id}&token={encoded}",
            UriKind.Absolute);
        var expiresAt = clock.UtcNow.Add(SetPasswordTokenLifetime);

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = $"{request.InvitingOrganizationName} invited you to Lagedra",
            HtmlBody = BuildHtmlBody(request, setPasswordUrl, expiresAt),
            PlainTextBody = BuildPlainTextBody(request, setPasswordUrl, expiresAt)
        }, ct).ConfigureAwait(false);

        LogInvited(logger, request.Email, request.InvitingOrganizationName, user.Id);

        return Result<InvitedUserDto>.Success(new InvitedUserDto(
            user.Id,
            user.Email!,
            WasJustCreated: true,
            setPasswordUrl,
            expiresAt));
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var trimmed = fullName.Trim();
        var idx = trimmed.IndexOf(' ', StringComparison.Ordinal);
        return idx <= 0
            ? (trimmed, string.Empty)
            : (trimmed[..idx], trimmed[(idx + 1)..].Trim());
    }

    private static string GenerateRandomPassword()
    {
        // 24 random bytes → 32-char base64. Identity password policy requires upper, lower,
        // digit, length 8+; base64 of random bytes effectively always satisfies it, and we
        // append fixed satisfiers as a defensive measure to avoid one-in-a-million rejections.
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        var rand = Convert.ToBase64String(bytes);
        return $"Aa1!{rand}";
    }

    private static string BuildHtmlBody(InvitedUserRequest request, Uri setPasswordUrl, DateTime expiresAt)
    {
        var orgEncoded = System.Net.WebUtility.HtmlEncode(request.InvitingOrganizationName);
        var nameEncoded = System.Net.WebUtility.HtmlEncode(request.FullName);
        var expiresText = expiresAt.ToString("yyyy-MM-dd HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
        return $"""
            <h2>You've been invited to Lagedra</h2>
            <p>{orgEncoded} has created a Lagedra account for you, {nameEncoded}.</p>
            <p>Click the link below to set your password and finish activating your account.</p>
            <p><a href="{setPasswordUrl}">Set your password</a></p>
            <p>This link expires on {expiresText}. If it expires you can request a new one from the Lagedra sign-in page using the "Forgot password" link.</p>
            <hr/>
            <p style="color:#666;font-size:12px;">If you don't recognise {orgEncoded} you can safely ignore this email; the account will be removed automatically if you do not set a password within 7 days.</p>
            """;
    }

    private static string BuildPlainTextBody(InvitedUserRequest request, Uri setPasswordUrl, DateTime expiresAt)
    {
        var expiresText = expiresAt.ToString("yyyy-MM-dd HH:mm 'UTC'", System.Globalization.CultureInfo.InvariantCulture);
        return $"""
            You've been invited to Lagedra by {request.InvitingOrganizationName}.

            Set your password: {setPasswordUrl}
            Link expires: {expiresText}

            If you don't recognise {request.InvitingOrganizationName} you can ignore this email.
            """;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Created invited user {UserId} for email {Email} on behalf of '{OrgName}'")]
    private static partial void LogInvited(ILogger logger, string email, string orgName, Guid userId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Invitation requested for {Email}, but a user with that email already exists. Returning existing user id (idempotent).")]
    private static partial void LogExistingUserFound(ILogger logger, string email);
}
