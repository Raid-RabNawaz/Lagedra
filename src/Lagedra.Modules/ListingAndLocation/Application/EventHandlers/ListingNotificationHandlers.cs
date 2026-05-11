using Lagedra.Modules.ListingAndLocation.Domain.Events;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;

namespace Lagedra.Modules.ListingAndLocation.Application.EventHandlers;

public sealed class OnListingSubmittedForReviewNotify(IMediator m)
    : IDomainEventHandler<ListingSubmittedForReviewEvent>
{
    private static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];

    public async Task Handle(ListingSubmittedForReviewEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.LandlordUserId, "listing_submitted_for_review",
            "Listing submitted for review",
            "Thanks! Our team will review your listing and email you once it's approved.",
            new() { ["listingId"] = e.ListingId.ToString() },
            InAppOnly, e.ListingId, "Listing"), ct).ConfigureAwait(false);
    }
}

public sealed class OnListingPublishedNotify(IMediator m)
    : IDomainEventHandler<ListingPublishedEvent>
{
    private static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];

    public async Task Handle(ListingPublishedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.LandlordUserId, "listing_published",
            "Listing approved & published",
            "Your listing has been approved and is now live in the marketplace.",
            new() { ["listingId"] = e.ListingId.ToString() },
            InAppOnly, e.ListingId, "Listing"), ct).ConfigureAwait(false);
    }
}

public sealed class OnListingDeniedNotify(IMediator m)
    : IDomainEventHandler<ListingDeniedEvent>
{
    private static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];

    public async Task Handle(ListingDeniedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.LandlordUserId, "listing_denied",
            "Listing needs changes",
            $"An admin couldn't approve your listing. Reason: {e.Reason}. Update the listing and submit it again, or delete it.",
            new() { ["listingId"] = e.ListingId.ToString(), ["reason"] = e.Reason },
            InAppOnly, e.ListingId, "Listing"), ct).ConfigureAwait(false);
    }
}
