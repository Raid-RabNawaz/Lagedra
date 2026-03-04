using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.Auth.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

public sealed record VerifyEmailCommand(Guid UserId, string Token) : IRequest<Result>;

public sealed class VerifyEmailCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEventBus eventBus,
    IClock clock)
    : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
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
            Role: user.Role), cancellationToken).ConfigureAwait(true);

        return Result.Success();
    }
}
