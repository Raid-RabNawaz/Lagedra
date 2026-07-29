using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Emails;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Auth.Application.Commands;

public sealed record ResendVerificationCommand(string Email) : IRequest<Result<ResendVerificationResultDto>>;

public sealed class ResendVerificationCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IConfiguration configuration)
    : IRequestHandler<ResendVerificationCommand, Result<ResendVerificationResultDto>>
{
    public async Task<Result<ResendVerificationResultDto>> Handle(
        ResendVerificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(true);

        // Return success regardless to prevent email enumeration
        if (user is null)
        {
            return Result<ResendVerificationResultDto>.Success(
                ResendVerificationResultDto.Blind());
        }

        if (await userManager.IsEmailConfirmedAsync(user).ConfigureAwait(true))
        {
            return Result<ResendVerificationResultDto>.Success(
                ResendVerificationResultDto.Blind());
        }

        var rawToken = await userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(true);
        var encodedToken = Uri.EscapeDataString(rawToken);
        var frontendUrl = configuration["App:FrontendUrl"] ?? "http://localhost:5173";
        var verifyUrl = WelcomeEmailComposer.BuildSpaVerifyUrl(frontendUrl, user.Id, encodedToken);

        // Match the original welcome email: password-less founding hosts get
        // the set-password next-step copy; everyone else gets role-aware welcome.
        var hasPassword = await userManager.HasPasswordAsync(user).ConfigureAwait(true);
        var email = WelcomeEmailComposer.BuildVerificationEmail(user, verifyUrl, hasPassword);

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = email.Subject,
            HtmlBody = email.HtmlBody,
            PlainTextBody = email.PlainTextBody
        }, cancellationToken).ConfigureAwait(true);

        return Result<ResendVerificationResultDto>.Success(
            new ResendVerificationResultDto(
                Sent: true,
                VerificationUrl: verifyUrl,
                VerificationToken: rawToken));
    }
}
