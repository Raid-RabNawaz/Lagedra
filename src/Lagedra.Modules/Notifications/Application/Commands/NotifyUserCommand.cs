using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.Notifications.Application.Commands;

public sealed record NotifyUserCommand(
    Guid RecipientUserId,
    string TemplateId,
    string Title,
    string Body,
    Dictionary<string, string> Payload,
    IReadOnlyList<NotificationChannel> Channels,
    Guid? RelatedEntityId = null,
    string? RelatedEntityType = null) : IRequest<Result>;

public sealed partial class NotifyUserCommandHandler(
    IMediator mediator,
    IUserEmailResolver emailResolver,
    ILogger<NotifyUserCommandHandler> logger)
    : IRequestHandler<NotifyUserCommand, Result>
{
    public async Task<Result> Handle(NotifyUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        foreach (var channel in request.Channels)
        {
            switch (channel)
            {
                case NotificationChannel.Email:
                    var email = await emailResolver
                        .GetEmailAsync(request.RecipientUserId, cancellationToken)
                        .ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        LogEmailNotFound(logger, request.RecipientUserId);
                        break;
                    }

                    await mediator.Send(new QueueNotificationCommand(
                        request.RecipientUserId,
                        email,
                        NotificationChannel.Email,
                        request.TemplateId,
                        request.Payload), cancellationToken).ConfigureAwait(false);
                    break;

                case NotificationChannel.InApp:
                    await mediator.Send(new DeliverInAppNotificationCommand(
                        request.RecipientUserId,
                        request.Title,
                        request.Body,
                        request.TemplateId,
                        request.RelatedEntityId,
                        request.RelatedEntityType), cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Could not resolve email for user {UserId}, skipping email notification")]
    private static partial void LogEmailNotFound(ILogger logger, Guid userId);
}
