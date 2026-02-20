using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Application.Services;
using Lagedra.Auth.Domain;
using Lagedra.Auth.Infrastructure.Seed;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Lagedra.Auth.Application.Commands;

public sealed record LoginCommand(string Email, string Password, string IpAddress) : IRequest<Result<AuthResultDto>>;

public sealed class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    JwtTokenService jwtTokenService,
    RefreshTokenService refreshTokenService,
    IClock clock,
    IOptions<SuperAdminSettings> superAdminOptions)
    : IRequestHandler<LoginCommand, Result<AuthResultDto>>
{
    private readonly SuperAdminSettings _superAdmin = superAdminOptions.Value;

    public async Task<Result<AuthResultDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Superadmin bypass: credentials validated against config, not DB.
        // This allows access even if the DB account is deactivated or corrupted.
        if (IsSuperAdminRequest(request))
        {
            return await IssueSuperAdminTokenAsync(request, cancellationToken).ConfigureAwait(true);
        }

        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(true);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        if (!user.IsActive)
        {
            return AuthErrors.AccountInactive;
        }

        var emailConfirmed = await userManager.IsEmailConfirmedAsync(user).ConfigureAwait(true);
        if (!emailConfirmed)
        {
            return AuthErrors.EmailNotVerified;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(true);
        if (!passwordValid)
        {
            return AuthErrors.InvalidCredentials;
        }

        return await BuildTokenResultAsync(user, request.IpAddress, cancellationToken).ConfigureAwait(true);
    }

    private bool IsSuperAdminRequest(LoginCommand request) =>
        !string.IsNullOrWhiteSpace(_superAdmin.Password)
        && request.Email.Equals(_superAdmin.Email, StringComparison.OrdinalIgnoreCase)
        && request.Password == _superAdmin.Password;

    private async Task<Result<AuthResultDto>> IssueSuperAdminTokenAsync(LoginCommand request, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(request.Email).ConfigureAwait(true);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        return await BuildTokenResultAsync(user, request.IpAddress, ct).ConfigureAwait(true);
    }

    private async Task<Result<AuthResultDto>> BuildTokenResultAsync(
        ApplicationUser user,
        string ipAddress,
        CancellationToken ct)
    {
        user.LastLoginAt = clock.UtcNow;
        await userManager.UpdateAsync(user).ConfigureAwait(true);

        var accessToken = jwtTokenService.GenerateAccessToken(user);
        var (_, rawToken) = await refreshTokenService.CreateAsync(user.Id, ipAddress, ct).ConfigureAwait(true);

        return Result<AuthResultDto>.Success(new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: rawToken,
            ExpiresIn: jwtTokenService.ExpirySeconds,
            Role: user.Role));
    }
}
