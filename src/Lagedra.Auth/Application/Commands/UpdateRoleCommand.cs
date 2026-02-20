using Lagedra.Auth.Application.Errors;
using Lagedra.Auth.Domain;
using Lagedra.Auth.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Lagedra.Auth.Application.Commands;

public sealed record UpdateRoleCommand(Guid RequestingAdminId, Guid TargetUserId, UserRole NewRole) : IRequest<Result>;

public sealed class UpdateRoleCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEventBus eventBus,
    IClock clock)
    : IRequestHandler<UpdateRoleCommand, Result>
{
    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.RequestingAdminId == request.TargetUserId)
        {
            return AuthErrors.SelfRoleElevation;
        }

        var user = await userManager.FindByIdAsync(request.TargetUserId.ToString()).ConfigureAwait(true);
        if (user is null)
        {
            return AuthErrors.UserNotFound;
        }

        var oldRole = user.Role;
        user.Role = request.NewRole;

        var result = await userManager.UpdateAsync(user).ConfigureAwait(true);
        if (!result.Succeeded)
        {
            return AuthErrors.IdentityError(result.Errors.First().Description);
        }

        await eventBus.Publish(new UserRoleChangedEvent(
            EventId: Guid.NewGuid(),
            OccurredAt: clock.UtcNow,
            UserId: user.Id,
            OldRole: oldRole,
            NewRole: request.NewRole), cancellationToken).ConfigureAwait(true);

        return Result.Success();
    }
}
