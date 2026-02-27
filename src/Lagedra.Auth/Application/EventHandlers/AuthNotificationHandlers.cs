using Lagedra.Auth.Domain.Events;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;

namespace Lagedra.Auth.Application.EventHandlers;

public sealed class OnUserRegisteredNotify(IMediator m)
    : IDomainEventHandler<UserRegisteredEvent>
{
    private static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];

    public async Task Handle(UserRegisteredEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.UserId, "welcome",
            "Welcome to Lagedra",
            "Your account has been created. Complete your profile to get started.",
            new() { ["email"] = e.Email },
            InAppOnly), ct).ConfigureAwait(false);
    }
}
