using Lagedra.Compliance.Domain.Events;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;

namespace Lagedra.Compliance.Application.EventHandlers;

public sealed class OnViolationCreatedNotify(IMediator mediator)
    : IDomainEventHandler<ViolationCreatedEvent>
{
    public async Task Handle(ViolationCreatedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        await mediator.Send(new NotifyUserCommand(
            e.TargetUserId, "compliance_violation_created",
            "Compliance Violation Recorded",
            $"A {e.Category} violation has been recorded on your deal.",
            new() { ["violationId"] = e.ViolationId.ToString(), ["dealId"] = e.DealId.ToString(), ["category"] = e.Category.ToString() },
            [NotificationChannel.Email, NotificationChannel.InApp],
            e.ViolationId, "Violation"), ct).ConfigureAwait(false);
    }
}

public sealed class OnViolationResolvedNotify(IMediator mediator)
    : IDomainEventHandler<ViolationResolvedEvent>
{
    public async Task Handle(ViolationResolvedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        await mediator.Send(new NotifyUserCommand(
            e.TargetUserId, "compliance_violation_resolved",
            "Violation Resolved",
            "A compliance violation on your deal has been resolved.",
            new() { ["violationId"] = e.ViolationId.ToString(), ["dealId"] = e.DealId.ToString() },
            [NotificationChannel.Email, NotificationChannel.InApp],
            e.ViolationId, "Violation"), ct).ConfigureAwait(false);
    }
}

public sealed class OnViolationEscalatedNotify(IMediator mediator)
    : IDomainEventHandler<ViolationEscalatedEvent>
{
    public async Task Handle(ViolationEscalatedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        await mediator.Send(new NotifyUserCommand(
            e.TargetUserId, "compliance_violation_escalated",
            "Violation Escalated",
            $"A {e.Category} violation on your deal has been escalated for further review.",
            new() { ["violationId"] = e.ViolationId.ToString(), ["dealId"] = e.DealId.ToString(), ["category"] = e.Category.ToString() },
            [NotificationChannel.Email, NotificationChannel.InApp],
            e.ViolationId, "Violation"), ct).ConfigureAwait(false);
    }
}
