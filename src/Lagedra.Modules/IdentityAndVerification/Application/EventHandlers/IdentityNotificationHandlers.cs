using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;

namespace Lagedra.Modules.IdentityAndVerification.Application.EventHandlers;

internal static class NotifyChannels
{
    internal static readonly NotificationChannel[] EmailAndInApp = [NotificationChannel.Email, NotificationChannel.InApp];
    internal static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];
}

public sealed class OnIdentityVerifiedNotify(IMediator m)
    : IDomainEventHandler<IdentityVerifiedEvent>
{
    public async Task Handle(IdentityVerifiedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.UserId, "identity_verified",
            "Identity Verified",
            "Your identity has been successfully verified.",
            new() { ["userId"] = e.UserId.ToString() },
            NotifyChannels.EmailAndInApp), ct).ConfigureAwait(false);
    }
}

public sealed class OnIdentityVerificationFailedNotify(IMediator m)
    : IDomainEventHandler<IdentityVerificationFailedEvent>
{
    public async Task Handle(IdentityVerificationFailedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.UserId, "identity_verification_failed",
            "Identity Verification Failed",
            $"Your identity verification could not be completed: {e.Reason}",
            new() { ["userId"] = e.UserId.ToString(), ["reason"] = e.Reason },
            NotifyChannels.EmailAndInApp), ct).ConfigureAwait(false);
    }
}

public sealed class OnVerificationClassChangedNotify(IMediator m)
    : IDomainEventHandler<VerificationClassChangedEvent>
{
    public async Task Handle(VerificationClassChangedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.UserId, "verification_class_changed",
            "Verification Level Updated",
            $"Your verification level has been updated from {e.OldClass} to {e.NewClass}.",
            new() { ["oldClass"] = e.OldClass.ToString(), ["newClass"] = e.NewClass.ToString() },
            NotifyChannels.InAppOnly), ct).ConfigureAwait(false);
    }
}
