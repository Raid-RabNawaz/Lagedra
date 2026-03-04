using Lagedra.Auth.Application.DTOs;
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
        var baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5000";
        var verifyUrl = $"{baseUrl}/v1/auth/verify-email?userId={user.Id}&token={encodedToken}";

        await emailService.SendAsync(new EmailMessage
        {
            To = user.Email!,
            Subject = "Verify your Lagedra account",
            HtmlBody = $"""
                <h2>Email Verification</h2>
                <p>You requested a new verification link. Click below to verify your email address.</p>
                <p><a href="{verifyUrl}">Verify Email</a></p>
                <p>This link expires in 24 hours. If you did not request this, you can safely ignore this email.</p>
                """,
            PlainTextBody = $"Verify your email: {verifyUrl}"
        }, cancellationToken).ConfigureAwait(true);

        return Result<ResendVerificationResultDto>.Success(
            new ResendVerificationResultDto(
                Sent: true,
                VerificationUrl: new Uri(verifyUrl),
                VerificationToken: rawToken));
    }
}
