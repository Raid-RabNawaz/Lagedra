using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.TruthSurface.Domain.Events;
using MediatR;

namespace Lagedra.TruthSurface.Application.EventHandlers;

internal static class TruthSurfaceChannels
{
    internal static readonly NotificationChannel[] EmailAndInApp =
        [NotificationChannel.Email, NotificationChannel.InApp];
}

public sealed class OnTruthSurfaceInitiatedNotify(
    IDealApplicationStatusProvider dealProvider,
    IMediator mediator)
    : IDomainEventHandler<TruthSurfaceInitiatedEvent>
{
    public async Task Handle(TruthSurfaceInitiatedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var participants = await dealProvider
            .GetParticipantsAsync(domainEvent.DealId, ct)
            .ConfigureAwait(false);

        if (participants is null) return;

        var data = new Dictionary<string, string>
        {
            ["snapshotId"] = domainEvent.SnapshotId.ToString(),
            ["dealId"] = domainEvent.DealId.ToString()
        };

        await mediator.Send(new NotifyUserCommand(
            participants.LandlordUserId, "truth_surface_initiated",
            "Deal Terms Ready for Review",
            "The deal terms snapshot is ready for your confirmation.",
            data, TruthSurfaceChannels.EmailAndInApp,
            domainEvent.DealId, "Deal"), ct).ConfigureAwait(false);

        await mediator.Send(new NotifyUserCommand(
            participants.TenantUserId, "truth_surface_initiated",
            "Deal Terms Ready for Review",
            "The deal terms snapshot is ready for your confirmation.",
            data, TruthSurfaceChannels.EmailAndInApp,
            domainEvent.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}

public sealed class OnTruthSurfaceConfirmedNotify(
    IDealApplicationStatusProvider dealProvider,
    IMediator mediator)
    : IDomainEventHandler<TruthSurfaceConfirmedEvent>
{
    public async Task Handle(TruthSurfaceConfirmedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var participants = await dealProvider
            .GetParticipantsAsync(domainEvent.DealId, ct)
            .ConfigureAwait(false);

        if (participants is null) return;

        var data = new Dictionary<string, string>
        {
            ["snapshotId"] = domainEvent.SnapshotId.ToString(),
            ["dealId"] = domainEvent.DealId.ToString()
        };

        await mediator.Send(new NotifyUserCommand(
            participants.LandlordUserId, "truth_surface_confirmed",
            "Deal Terms Confirmed",
            "Both parties have confirmed the deal terms. The snapshot has been sealed.",
            data, TruthSurfaceChannels.EmailAndInApp,
            domainEvent.DealId, "Deal"), ct).ConfigureAwait(false);

        await mediator.Send(new NotifyUserCommand(
            participants.TenantUserId, "truth_surface_confirmed",
            "Deal Terms Confirmed",
            "Both parties have confirmed the deal terms. The snapshot has been sealed.",
            data, TruthSurfaceChannels.EmailAndInApp,
            domainEvent.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}

public sealed class OnTruthSurfaceSupersededNotify(
    IDealApplicationStatusProvider dealProvider,
    IMediator mediator)
    : IDomainEventHandler<TruthSurfaceSupersededEvent>
{
    public async Task Handle(TruthSurfaceSupersededEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var participants = await dealProvider
            .GetParticipantsAsync(domainEvent.DealId, ct)
            .ConfigureAwait(false);

        if (participants is null) return;

        var data = new Dictionary<string, string>
        {
            ["originalSnapshotId"] = domainEvent.OriginalSnapshotId.ToString(),
            ["newSnapshotId"] = domainEvent.SupersedingSnapshotId.ToString(),
            ["dealId"] = domainEvent.DealId.ToString(),
            ["reason"] = domainEvent.Reason
        };

        await mediator.Send(new NotifyUserCommand(
            participants.LandlordUserId, "truth_surface_superseded",
            "Deal Terms Updated",
            $"The deal terms have been updated. Reason: {domainEvent.Reason}. Please review and confirm the new terms.",
            data, TruthSurfaceChannels.EmailAndInApp,
            domainEvent.DealId, "Deal"), ct).ConfigureAwait(false);

        await mediator.Send(new NotifyUserCommand(
            participants.TenantUserId, "truth_surface_superseded",
            "Deal Terms Updated",
            $"The deal terms have been updated. Reason: {domainEvent.Reason}. Please review and confirm the new terms.",
            data, TruthSurfaceChannels.EmailAndInApp,
            domainEvent.DealId, "Deal"), ct).ConfigureAwait(false);
    }
}
