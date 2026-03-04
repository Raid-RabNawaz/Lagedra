using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Application.Services;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Lagedra.Auth.Application.Commands;

public sealed record ExternalLoginCommand(
    ExternalAuthProvider Provider,
    string IdToken,
    UserRole? PreferredRole,
    string IpAddress) : IRequest<Result<AuthResultDto>>;

public sealed partial class ExternalLoginCommandHandler(
    ExternalAuthValidator externalAuthValidator,
    UserManager<ApplicationUser> userManager,
    JwtTokenService jwtTokenService,
    RefreshTokenService refreshTokenService,
    IClock clock,
    ILogger<ExternalLoginCommandHandler> logger)
    : IRequestHandler<ExternalLoginCommand, Result<AuthResultDto>>
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to link {Provider} login to existing user {UserId}: {Errors}")]
    private partial void LogLinkFailed(string provider, Guid userId, string errors);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to add {Provider} login for new user {UserId}: {Errors}")]
    private partial void LogAddLoginFailed(string provider, Guid userId, string errors);
    public async Task<Result<AuthResultDto>> Handle(
        ExternalLoginCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var externalUser = await externalAuthValidator
            .ValidateAsync(request.Provider, request.IdToken, cancellationToken)
            .ConfigureAwait(false);

        if (externalUser is null)
        {
            return AuthErrors.InvalidToken;
        }

        var providerName = request.Provider.ToString();

        var user = await userManager
            .FindByLoginAsync(providerName, externalUser.ProviderKey)
            .ConfigureAwait(false);

        if (user is not null)
        {
            return await IssueTokensAsync(user, request.IpAddress, cancellationToken)
                .ConfigureAwait(false);
        }

        user = await userManager.FindByEmailAsync(externalUser.Email).ConfigureAwait(false);

        if (user is not null)
        {
            var linkResult = await userManager
                .AddLoginAsync(user, new UserLoginInfo(providerName, externalUser.ProviderKey, providerName))
                .ConfigureAwait(false);

            if (!linkResult.Succeeded)
            {
                LogLinkFailed(providerName, user.Id,
                    string.Join(", ", linkResult.Errors.Select(e => e.Description)));

                return AuthErrors.IdentityError("Failed to link external account.");
            }

            return await IssueTokensAsync(user, request.IpAddress, cancellationToken)
                .ConfigureAwait(false);
        }

        var role = request.PreferredRole ?? UserRole.Tenant;
        if (role is UserRole.Arbitrator or UserRole.PlatformAdmin or UserRole.InsurancePartner)
        {
            role = UserRole.Tenant;
        }

        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = externalUser.Email,
            Email = externalUser.Email,
            EmailConfirmed = true,
            Role = role,
            IsActive = true,
            CreatedAt = clock.UtcNow
        };

        var createResult = await userManager.CreateAsync(newUser).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            return AuthErrors.IdentityError(createResult.Errors.First().Description);
        }

        var addLoginResult = await userManager
            .AddLoginAsync(newUser, new UserLoginInfo(providerName, externalUser.ProviderKey, providerName))
            .ConfigureAwait(false);

        if (!addLoginResult.Succeeded)
        {
            LogAddLoginFailed(providerName, newUser.Id,
                string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
        }

        return await IssueTokensAsync(newUser, request.IpAddress, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<Result<AuthResultDto>> IssueTokensAsync(
        ApplicationUser user,
        string ipAddress,
        CancellationToken ct)
    {
        if (!user.IsActive)
        {
            return AuthErrors.AccountInactive;
        }

        user.LastLoginAt = clock.UtcNow;
        await userManager.UpdateAsync(user).ConfigureAwait(false);

        var accessToken = await jwtTokenService.GenerateAccessTokenAsync(user).ConfigureAwait(false);
        var (_, rawToken) = await refreshTokenService.CreateAsync(user.Id, ipAddress, ct).ConfigureAwait(false);

        return Result<AuthResultDto>.Success(new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: rawToken,
            ExpiresIn: jwtTokenService.ExpirySeconds,
            Role: user.Role));
    }
}
