using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

public sealed record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest<Result>;

public sealed class ChangePasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return AuthErrors.IdentityError(result.Errors.First().Description);
        }

        return Result.Success();
    }
}
