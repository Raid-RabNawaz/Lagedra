using Lagedra.Modules.ListingAndLocation.Domain.Events;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;

namespace Lagedra.Modules.ListingAndLocation.Application.EventHandlers;

public sealed class OnListingPublishedNotify(IMediator m)
    : IDomainEventHandler<ListingPublishedEvent>
{
    private static readonly NotificationChannel[] InAppOnly = [NotificationChannel.InApp];

    public async Task Handle(ListingPublishedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);
        await m.Send(new NotifyUserCommand(
            e.LandlordUserId, "listing_published",
            "Listing Published",
            "Your listing is now live and visible to tenants.",
            new() { ["listingId"] = e.ListingId.ToString() },
            InAppOnly, e.ListingId, "Listing"), ct).ConfigureAwait(false);
    }
}
