using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.StructuredInquiry.Application.EventHandlers;

public sealed partial class OnInquiryPartnerQuestionSubmittedNotifyHandler(
    IMediator mediator,
    IListingProvider listingProvider,
    IConfiguration configuration,
    ILogger<OnInquiryPartnerQuestionSubmittedNotifyHandler> logger)
    : IDomainEventHandler<InquiryPartnerQuestionSubmittedEvent>
{
    private static readonly NotificationChannel[] EmailAndInApp =
        [NotificationChannel.Email, NotificationChannel.InApp];

    public async Task Handle(InquiryPartnerQuestionSubmittedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        var listing = await listingProvider
            .GetListingDetailsAsync(e.ListingId, ct)
            .ConfigureAwait(false);

        var listingTitle = listing?.Title ?? "a listing";
        var frontendUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:3000")
            .TrimEnd('/');
        var threadUrl = $"{frontendUrl}/app/inquiry/{e.SessionId}";

        var body =
            $"A partner asked a new question about {listingTitle}. Open the thread to respond.";

        var data = new Dictionary<string, string>
        {
            ["sessionId"] = e.SessionId.ToString(),
            ["questionId"] = e.QuestionId.ToString(),
            ["listingId"] = e.ListingId.ToString(),
            ["listingTitle"] = listingTitle,
            ["threadUrl"] = threadUrl,
            ["frontendUrl"] = frontendUrl,
        };

        foreach (var recipientId in new[] { e.TenantUserId, e.LandlordUserId }.Distinct())
        {
            LogNotifying(logger, e.SessionId, e.QuestionId, recipientId);

            await mediator.Send(new NotifyUserCommand(
                recipientId,
                "inquiry_partner_question",
                "Partner asked a question",
                body,
                data,
                EmailAndInApp,
                e.SessionId,
                "InquirySession"), ct).ConfigureAwait(false);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Notifying user {RecipientUserId} of partner question {QuestionId} on session {SessionId}.")]
    private static partial void LogNotifying(
        ILogger logger, Guid sessionId, Guid questionId, Guid recipientUserId);
}
