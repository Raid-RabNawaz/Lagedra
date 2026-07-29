using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.Auth.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

public sealed record VerifyEmailCommand(Guid UserId, string Token) : IRequest<Result<VerifyEmailResultDto>>;

/// <summary>
/// <paramref name="PasswordSetupToken"/> is only issued for accounts created
/// without a password (pre-launch founding hosts): possession of the email
/// confirmation token already proves mailbox control, so handing back a
/// password-reset token lets the SPA chain straight into "set your password".
/// </summary>
public sealed record VerifyEmailResultDto(
    bool RequiresPasswordSetup,
    string? PasswordSetupToken);

public sealed class VerifyEmailCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEventBus eventBus,
    IClock clock)
    : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResultDto>>
{
    public async Task<Result<VerifyEmailResultDto>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var user = await userManager.FindByIdAsync(request.UserId.ToString()).ConfigureAwait(true);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var result = await userManager.ConfirmEmailAsync(user, request.Token).ConfigureAwait(true);
        if (!result.Succeeded)
        {
            return AuthErrors.InvalidToken;
        }

        user.IsActive = true;
        await userManager.UpdateAsync(user).ConfigureAwait(true);

        await eventBus.Publish(new UserRegisteredEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: clock.UtcNow,
            UserId: user.Id,
            Email: user.Email!,
            Role: user.Role,
            SignupType: user.SignupType), cancellationToken).ConfigureAwait(true);

        await eventBus.Publish(
            new SharedKernel.Integration.Events.EmailVerifiedEvent(user.Id, clock.UtcNow),
            cancellationToken).ConfigureAwait(true);

        var hasPassword = await userManager.HasPasswordAsync(user).ConfigureAwait(true);
        if (hasPassword)
        {
            return Result<VerifyEmailResultDto>.Success(
                new VerifyEmailResultDto(RequiresPasswordSetup: false, PasswordSetupToken: null));
        }

        var setupToken = await userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(true);
        return Result<VerifyEmailResultDto>.Success(
            new VerifyEmailResultDto(RequiresPasswordSetup: true, PasswordSetupToken: setupToken));
    }
}
