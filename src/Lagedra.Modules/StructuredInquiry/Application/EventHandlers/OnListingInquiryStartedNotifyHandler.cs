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
/// Phase 17 — emails the host (and posts an in-app notification) the
/// moment a tenant starts a pre-booking inquiry thread on their listing.
/// Uses the <c>inquiry_started</c> template seeded by
/// <see cref="Lagedra.Modules.Notifications.Infrastructure.Seeding.NotificationTemplateSeeder"/>.
/// </summary>
public sealed partial class OnListingInquiryStartedNotifyHandler(
    IMediator mediator,
    IListingProvider listingProvider,
    IConfiguration configuration,
    ILogger<OnListingInquiryStartedNotifyHandler> logger)
    : IDomainEventHandler<ListingInquiryStartedEvent>
{
    private static readonly NotificationChannel[] EmailAndInApp =
        [NotificationChannel.Email, NotificationChannel.InApp];

    public async Task Handle(ListingInquiryStartedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        var listing = await listingProvider
            .GetListingDetailsAsync(e.ListingId, ct)
            .ConfigureAwait(false);

        var listingTitle = listing?.Title ?? "your listing";
        var frontendUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:3000")
            .TrimEnd('/');
        var threadUrl = $"{frontendUrl}/app/inquiry/{e.SessionId}";

        LogNotifying(logger, e.SessionId, e.LandlordUserId);

        await mediator.Send(new NotifyUserCommand(
            e.LandlordUserId,
            "inquiry_started",
            "New question about your listing",
            $"A guest has started a conversation about {listingTitle}. Open the thread to respond.",
            new()
            {
                ["sessionId"] = e.SessionId.ToString(),
                ["listingId"] = e.ListingId.ToString(),
                ["listingTitle"] = listingTitle,
                ["threadUrl"] = threadUrl,
                ["frontendUrl"] = frontendUrl,
            },
            EmailAndInApp,
            e.SessionId,
            "InquirySession"), ct).ConfigureAwait(false);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Notifying host {LandlordUserId} of new inquiry session {SessionId}.")]
    private static partial void LogNotifying(
        ILogger logger, Guid sessionId, Guid landlordUserId);
}
