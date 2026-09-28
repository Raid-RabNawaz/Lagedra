using System.Net;
using Lagedra.Modules.Notifications.Application.Commands;
using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.StructuredInquiry.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.StructuredInquiry.Application.EventHandlers;

public sealed partial class OnInquiryTenantQuestionSubmittedNotifyHandler(
    IMediator mediator,
    IListingProvider listingProvider,
    IConfiguration configuration,
    ILogger<OnInquiryTenantQuestionSubmittedNotifyHandler> logger)
    : IDomainEventHandler<InquiryTenantQuestionSubmittedEvent>
{
    private static readonly NotificationChannel[] EmailAndInApp =
        [NotificationChannel.Email, NotificationChannel.InApp];

    public async Task Handle(InquiryTenantQuestionSubmittedEvent e, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(e);

        var listing = await listingProvider
            .GetListingDetailsAsync(e.ListingId, ct)
            .ConfigureAwait(false);

        var listingTitle = listing?.Title ?? "your listing";
        var frontendUrl = (configuration["App:FrontendUrl"] ?? "http://localhost:3000")
            .TrimEnd('/');
        var threadUrl = $"{frontendUrl}/app/inquiry/{e.SessionId}";
        var questionText = e.QuestionText.Trim();

        LogNotifying(logger, e.SessionId, e.LandlordUserId);

        await mediator.Send(new NotifyUserCommand(
            e.LandlordUserId,
            "inquiry_question",
            "New question about your listing",
            $"A guest asked about {listingTitle}: \"{questionText}\"",
            new()
            {
                ["sessionId"] = e.SessionId.ToString(),
                ["questionId"] = e.QuestionId.ToString(),
                ["listingId"] = e.ListingId.ToString(),
                ["listingTitle"] = listingTitle,
                ["questionText"] = questionText,
                ["questionTextHtml"] = WebUtility.HtmlEncode(questionText),
                ["threadUrl"] = threadUrl,
                ["frontendUrl"] = frontendUrl,
            },
            EmailAndInApp,
            e.SessionId,
            "InquirySession"), ct).ConfigureAwait(false);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Notifying host {LandlordUserId} of guest question on inquiry session {SessionId}.")]
    private static partial void LogNotifying(
        ILogger logger, Guid sessionId, Guid landlordUserId);
}
