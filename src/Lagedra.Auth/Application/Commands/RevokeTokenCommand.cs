using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Application.Services;
using Lagedra.SharedKernel.Results;
using MediatR;

namespace Lagedra.Auth.Application.Commands;

public sealed record RevokeTokenCommand(string RefreshToken, string IpAddress) : IRequest<Result>;

public sealed class RevokeTokenCommandHandler(RefreshTokenService refreshTokenService)
    : IRequestHandler<RevokeTokenCommand, Result>
{
    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var token = await refreshTokenService.GetActiveAsync(request.RefreshToken, cancellationToken).ConfigureAwait(true);
        if (token is null)
        {
            return AuthErrors.InvalidToken;
        }

        await refreshTokenService.RevokeAsync(token, request.IpAddress, ct: cancellationToken).ConfigureAwait(true);
        return Result.Success();
    }
}
