using System.Globalization;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Reviews.Domain.Events;
using Lagedra.SharedKernel.Events;
using MediatR;

namespace Lagedra.Modules.Reviews.Application.EventHandlers;

internal static class ReviewChannels
{
    internal static readonly NotificationChannel[] EmailInAppAndSms =
        [NotificationChannel.Email, NotificationChannel.InApp, NotificationChannel.Sms];
}

public sealed class OnStayReviewWindowOpenedNotify(IMediator mediator)
    : IDomainEventHandler<StayReviewWindowOpenedEvent>
{
    public async Task Handle(StayReviewWindowOpenedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var closes = domainEvent.ClosesAt.ToString("d", CultureInfo.InvariantCulture);
        var body =
            $"Your stay is complete. Please leave a review by {closes}. "
            + "We'll remind you if you haven't submitted yet. "
            + "Reviews stay private until both sides submit or the window closes.";

        await mediator.Send(new NotifyUserCommand(
            domainEvent.LandlordUserId,
            "review_due",
            "Leave a review for your guest",
            body,
            new() { ["dealId"] = domainEvent.DealId.ToString() },
            ReviewChannels.EmailInAppAndSms,
            domainEvent.DealId,
            "Deal"), ct).ConfigureAwait(false);

        await mediator.Send(new NotifyUserCommand(
            domainEvent.TenantUserId,
            "review_due",
            "Leave a review for your host",
            body,
            new() { ["dealId"] = domainEvent.DealId.ToString() },
            ReviewChannels.EmailInAppAndSms,
            domainEvent.DealId,
            "Deal"), ct).ConfigureAwait(false);
    }
}
