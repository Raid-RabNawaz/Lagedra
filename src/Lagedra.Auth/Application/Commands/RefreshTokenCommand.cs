using Lagedra.Auth.Application.DTOs;
using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Application.Services;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

public sealed record RefreshTokenCommand(string RefreshToken, string IpAddress) : IRequest<Result<AuthResultDto>>;

public sealed class RefreshTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    JwtTokenService jwtTokenService,
    RefreshTokenService refreshTokenService)
    : IRequestHandler<RefreshTokenCommand, Result<AuthResultDto>>
{
    public async Task<Result<AuthResultDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await refreshTokenService.GetActiveAsync(request.RefreshToken, cancellationToken).ConfigureAwait(true);
        if (existing is null)
        {
            return AuthErrors.InvalidToken;
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString()).ConfigureAwait(true);
        if (user is null || !user.IsActive)
        {
            return AuthErrors.AccountInactive;
        }

        // Rotate: revoke old, issue new
        var (_, newRaw) = await refreshTokenService.CreateAsync(user.Id, request.IpAddress, cancellationToken).ConfigureAwait(true);
        var newHash = newRaw; // already stored by service; revoke old pointing to new
        await refreshTokenService.RevokeAsync(existing, request.IpAddress, newHash, cancellationToken).ConfigureAwait(true);

        var accessToken = jwtTokenService.GenerateAccessToken(user);

        return Result<AuthResultDto>.Success(new AuthResultDto(
            AccessToken: accessToken,
            RefreshToken: newRaw,
            ExpiresIn: jwtTokenService.ExpirySeconds,
            Role: user.Role));
    }
}
