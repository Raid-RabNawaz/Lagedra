using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Email;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Lagedra.Auth.Application.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    UserRole Role) : IRequest<Result<RegisterResultDto>>;

public sealed class RegisterUserCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService,
    IClock clock,
    IConfiguration configuration)
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

        if (request.Role is UserRole.Arbitrator or UserRole.PlatformAdmin
            or UserRole.InsurancePartner or UserRole.InstitutionPartner)
        {
            return AuthErrors.IdentityError("Self-registration is only available for Tenant and Landlord roles.");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            Role = request.Role,
            IsActive = false,
            CreatedAt = clock.UtcNow
        };

        var identityResult = await userManager.CreateAsync(user, request.Password).ConfigureAwait(true);
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
        }, cancellationToken).ConfigureAwait(true);

        return Result<RegisterResultDto>.Success(
            new RegisterResultDto(user.Id, new Uri(verifyUrl), rawToken));
    }
}
