using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.StructuredInquiry.Application.EventHandlers;

public sealed partial class OnInquiryPartnerAddedNotifyHandler(
    IMediator mediator,
    IListingProvider listingProvider,
    IPartnerMembershipProvider membershipProvider,
    IConfiguration configuration,
    ILogger<OnInquiryPartnerAddedNotifyHandler> logger)
    : IDomainEventHandler<InquiryPartnerAddedEvent>
{
    private static readonly NotificationChannel[] EmailAndInApp =
        [NotificationChannel.Email, NotificationChannel.InApp];

    public async Task Handle(InquiryPartnerAddedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        var listing = await listingProvider
            .GetListingDetailsAsync(e.ListingId, ct)
            .ConfigureAwait(false);

        var listingTitle = listing?.Title ?? "a listing";
        var orgName = await membershipProvider
            .GetOrganizationNameAsync(e.PartnerOrganizationId, ct)
            .ConfigureAwait(false) ?? "your organization";

        var frontendUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:3000")
            .TrimEnd('/');
        var threadUrl = $"{frontendUrl}/app/inquiry/{e.SessionId}";

        var memberIds = await membershipProvider
            .GetMemberUserIdsAsync(e.PartnerOrganizationId, ct)
            .ConfigureAwait(false);

        foreach (var memberId in memberIds.Distinct())
        {
            LogNotifying(logger, e.SessionId, memberId);

            await mediator.Send(new NotifyUserCommand(
                memberId,
                "inquiry_partner_added",
                "Added to an inquiry",
                $"{orgName} was invited into a conversation about {listingTitle}. Open the thread to ask questions.",
                new()
                {
                    ["sessionId"] = e.SessionId.ToString(),
                    ["listingId"] = e.ListingId.ToString(),
                    ["listingTitle"] = listingTitle,
                    ["organizationId"] = e.PartnerOrganizationId.ToString(),
                    ["threadUrl"] = threadUrl,
                    ["frontendUrl"] = frontendUrl,
                },
                EmailAndInApp,
                e.SessionId,
                "InquirySession"), ct).ConfigureAwait(false);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Notifying partner staff {RecipientUserId} of partner add on session {SessionId}.")]
    private static partial void LogNotifying(ILogger logger, Guid sessionId, Guid recipientUserId);
}
