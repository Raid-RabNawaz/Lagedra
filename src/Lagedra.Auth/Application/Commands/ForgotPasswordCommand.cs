using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Auth.Application.Commands;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;

public sealed class ForgotPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IConfiguration configuration)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(true);

        // Always return success to prevent email enumeration
        if (user is null || !user.IsActive)
        {
            return Result.Success();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(true);
        var encoded = Uri.EscapeDataString(token);
        var baseUrl = configuration["App:FrontendUrl"] ?? "http://localhost:3000";
        var resetUrl = $"{baseUrl}/reset-password?userId={user.Id}&token={encoded}";

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "Reset your Lagedra password",
            HtmlBody = $"""
                <h2>Password Reset Request</h2>
                <p>Click the link below to reset your password. This link expires in 1 hour.</p>
                <p><a href="{resetUrl}">Reset Password</a></p>
                <p>If you did not request a password reset, you can safely ignore this email.</p>
                """,
            PlainTextBody = $"Reset your password: {resetUrl}"
        }, cancellationToken).ConfigureAwait(true);

        return Result.Success();
    }
}
