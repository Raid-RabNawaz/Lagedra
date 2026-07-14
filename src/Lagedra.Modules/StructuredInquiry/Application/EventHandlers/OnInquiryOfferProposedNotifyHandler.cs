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
/// Notifies the counterparty when a rent/deposit offer is proposed or countered
/// on an inquiry thread.
/// </summary>
public sealed partial class OnInquiryOfferProposedNotifyHandler(
    IMediator mediator,
    IListingProvider listingProvider,
    IConfiguration configuration,
    ILogger<OnInquiryOfferProposedNotifyHandler> logger)
    : IDomainEventHandler<InquiryOfferProposedEvent>
{
    private static readonly NotificationChannel[] EmailAndInApp =
        [NotificationChannel.Email, NotificationChannel.InApp];

    public async Task Handle(InquiryOfferProposedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        var recipientId = e.ProposedByUserId == e.TenantUserId
            ? e.LandlordUserId
            : e.TenantUserId;

        var listing = await listingProvider
            .GetListingDetailsAsync(e.ListingId, ct)
            .ConfigureAwait(false);

        var listingTitle = listing?.Title ?? "the listing";
        var frontendUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:3000")
            .TrimEnd('/');
        var threadUrl = $"{frontendUrl}/app/inquiry/{e.SessionId}";

        var rent = (e.RentCents / 100m).ToString("C", CultureInfo.InvariantCulture);
        var deposit = (e.DepositCents / 100m).ToString("C", CultureInfo.InvariantCulture);

        LogNotifying(logger, e.SessionId, e.OfferId, recipientId);

        await mediator.Send(new NotifyUserCommand(
            recipientId,
            "inquiry_offer_proposed",
            "New offer on your inquiry",
            $"An offer was proposed for {listingTitle}: rent {rent} and deposit {deposit}. Open the thread to accept or counter.",
            new()
            {
                ["sessionId"] = e.SessionId.ToString(),
                ["offerId"] = e.OfferId.ToString(),
                ["listingId"] = e.ListingId.ToString(),
                ["listingTitle"] = listingTitle,
                ["rentCents"] = e.RentCents.ToString(CultureInfo.InvariantCulture),
                ["depositCents"] = e.DepositCents.ToString(CultureInfo.InvariantCulture),
                ["threadUrl"] = threadUrl,
                ["frontendUrl"] = frontendUrl,
            },
            EmailAndInApp,
            e.SessionId,
            "InquirySession"), ct).ConfigureAwait(false);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Notifying user {RecipientUserId} of offer {OfferId} on session {SessionId}.")]
    private static partial void LogNotifying(
        ILogger logger, Guid sessionId, Guid offerId, Guid recipientUserId);
}
