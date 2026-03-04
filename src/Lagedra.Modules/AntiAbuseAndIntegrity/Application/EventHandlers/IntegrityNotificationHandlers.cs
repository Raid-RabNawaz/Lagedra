using Lagedra.Modules.AntiAbuseAndIntegrity.Domain.Events;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;

namespace Lagedra.Modules.AntiAbuseAndIntegrity.Application.EventHandlers;

public sealed class OnAccountRestrictionNotify(IMediator m)
    : IDomainEventHandler<AccountRestrictionAppliedEvent>
{
    private static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];

    public async Task Handle(AccountRestrictionAppliedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.UserId, "account_restricted",
            "Account Restriction Applied",
            $"A restriction has been applied to your account: {e.Reason}",
            new() { ["level"] = e.Level.ToString(), ["reason"] = e.Reason },
            InAppOnly), ct).ConfigureAwait(false);
    }
}
