using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword) : IRequest<Result>;

public sealed class ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(true);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword).ConfigureAwait(true);
        if (!result.Succeeded)
        {
            return AuthErrors.IdentityError(result.Errors.First().Description);
        }

        return Result.Success();
    }
}
