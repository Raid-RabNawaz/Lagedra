using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Security;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

public sealed record ConfirmPhoneVerificationCommand(Guid UserId, string Code) : IRequest<Result>;

public sealed class ConfirmPhoneVerificationCommandHandler(
    UserManager<ApplicationUser> userManager,
    IHashingService hashingService,
    IClock clock)
    : IRequestHandler<ConfirmPhoneVerificationCommand, Result>
{
    public async Task<Result> Handle(
        ConfirmPhoneVerificationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var code = request.Code?.Trim() ?? string.Empty;
        if (code.Length is < 4 or > 10)
        {
            return AuthErrors.InvalidPhoneCode;
        }

        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            return AuthErrors.PhoneRequired;
        }

        if (user.IsPhoneVerified)
        {
            return AuthErrors.PhoneAlreadyVerified;
        }

        if (string.IsNullOrWhiteSpace(user.PhoneVerificationCodeHash)
            || user.PhoneVerificationExpiresAt is null
            || clock.UtcNow > user.PhoneVerificationExpiresAt.Value
            || !hashingService.Verify(code, user.PhoneVerificationCodeHash))
        {
            return AuthErrors.InvalidPhoneCode;
        }

        user.IsPhoneVerified = true;
        user.PhoneNumberConfirmed = true;
        user.PhoneVerificationCodeHash = null;
        user.PhoneVerificationExpiresAt = null;

        var update = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!update.Succeeded)
        {
            return AuthErrors.IdentityError(update.Errors.First().Description);
        }

        return Result.Success();
    }
}
