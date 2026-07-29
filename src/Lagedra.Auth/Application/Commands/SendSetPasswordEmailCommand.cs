using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Auth.Application.Commands;

/// <summary>
/// Platform-admin action: email the user a one-time link to set/reset their
/// password. Used when the self-serve forgot-password flow isn't enough
/// (e.g. user never received the email, or an OAuth-only account needs a
/// password). Reuses the Identity password-reset token and the existing
/// <c>/auth/reset-password?setup=1</c> SPA page.
/// </summary>
public sealed record SendSetPasswordEmailCommand(
    Guid AdminUserId,
    Guid TargetUserId) : IRequest<Result>;

public sealed class SendSetPasswordEmailCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IConfiguration configuration)
    : IRequestHandler<SendSetPasswordEmailCommand, Result>
{
    public async Task<Result> Handle(SendSetPasswordEmailCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByIdAsync(request.TargetUserId.ToString()).ConfigureAwait(true);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return AuthErrors.IdentityError("This user does not have an email address.");
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(true);
        var encoded = Uri.EscapeDataString(token);
        var frontendUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:5173").TrimEnd('/');
        // setup=1 switches the SPA page to "Set your password" copy; the API
        // call is the same as a normal password reset.
        var setPasswordUrl =
            $"{frontendUrl}/auth/reset-password?userId={user.Id}&token={encoded}&setup=1";

        var greeting = string.IsNullOrWhiteSpace(user.FirstName)
            ? "Hi,"
            : $"Hi {user.FirstName},";

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email,
            Subject = "Set your Lagedra password",
            HtmlBody = $"""
                <!doctype html>
                <html>
                <body style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; max-width: 560px; margin: 0 auto; color: #1A1A2E;">
                  <h2 style="margin-top: 32px;">Set your password</h2>
                  <p>{greeting}</p>
                  <p>A Lagedra admin sent you this link so you can set a new password for your account.</p>
                  <p style="margin: 24px 0;">
                    <a href="{setPasswordUrl}"
                       style="display: inline-block; background: #5B3FE0; color: #fff; padding: 12px 20px; border-radius: 10px; text-decoration: none; font-weight: 600;">
                      Set password
                    </a>
                  </p>
                  <p style="font-size: 13px; color: #666;">This link expires soon. If you didn't expect this email, you can safely ignore it — your password will not change.</p>
                  <hr style="border: none; border-top: 1px solid #eee; margin: 32px 0;" />
                  <p style="font-size: 12px; color: #999;">
                    You're receiving this because a platform administrator requested a password setup email for {user.Email}.
                  </p>
                </body>
                </html>
                """,
            PlainTextBody = $"""
                Set your Lagedra password

                {greeting}

                A Lagedra admin sent you this link so you can set a new password for your account.

                Set password: {setPasswordUrl}

                If you didn't expect this email, you can safely ignore it.
                """
        }, cancellationToken).ConfigureAwait(true);

        return Result.Success();
    }
}
