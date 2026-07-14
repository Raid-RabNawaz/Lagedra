using System.Globalization;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.StructuredInquiry.Application.EventHandlers;

/// <summary>
/// Notifies both parties when an inquiry offer is accepted so they know
/// the agreed rent/deposit will be used at Apply.
/// </summary>
public sealed partial class OnInquiryOfferAcceptedNotifyHandler(
    IMediator mediator,
    IListingProvider listingProvider,
    IConfiguration configuration,
    ILogger<OnInquiryOfferAcceptedNotifyHandler> logger)
    : IDomainEventHandler<InquiryOfferAcceptedEvent>
{
    private static readonly NotificationChannel[] EmailAndInApp =
        [NotificationChannel.Email, NotificationChannel.InApp];

    public async Task Handle(InquiryOfferAcceptedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        var listing = await listingProvider
            .GetListingDetailsAsync(e.ListingId, ct)
            .ConfigureAwait(false);

        var listingTitle = listing?.Title ?? "the listing";
        var frontendUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:3000")
            .TrimEnd('/');
        var threadUrl = $"{frontendUrl}/app/inquiry/{e.SessionId}";

        var rent = (e.RentCents / 100m).ToString("C", CultureInfo.InvariantCulture);
        var deposit = (e.DepositCents / 100m).ToString("C", CultureInfo.InvariantCulture);
        var body =
            $"Terms agreed for {listingTitle}: rent {rent} and deposit {deposit}. These amounts will be used when the tenant applies.";

        var data = new Dictionary<string, string>
        {
            ["sessionId"] = e.SessionId.ToString(),
            ["offerId"] = e.OfferId.ToString(),
            ["listingId"] = e.ListingId.ToString(),
            ["listingTitle"] = listingTitle,
            ["rentCents"] = e.RentCents.ToString(CultureInfo.InvariantCulture),
            ["depositCents"] = e.DepositCents.ToString(CultureInfo.InvariantCulture),
            ["threadUrl"] = threadUrl,
            ["frontendUrl"] = frontendUrl,
        };

        foreach (var recipientId in new[] { e.TenantUserId, e.LandlordUserId }.Distinct())
        {
            LogNotifying(logger, e.SessionId, e.OfferId, recipientId);

            await mediator.Send(new NotifyUserCommand(
                recipientId,
                "inquiry_offer_accepted",
                "Offer accepted",
                body,
                data,
                EmailAndInApp,
                e.SessionId,
                "InquirySession"), ct).ConfigureAwait(false);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Notifying user {RecipientUserId} of accepted offer {OfferId} on session {SessionId}.")]
    private static partial void LogNotifying(
        ILogger logger, Guid sessionId, Guid offerId, Guid recipientUserId);
}
