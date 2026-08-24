using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.Notifications.Application.Commands;

/// <summary>
/// Records an asynchronous delivery outcome reported by Twilio's status
/// callback. Before this existed, carrier-side failures were invisible: the
/// Messaging API accepts a message (we log "SMS sent"), and only later marks
/// it undelivered — e.g. error 30034 (unregistered A2P 10DLC sender), which
/// silently ate every verification SMS on Aug 10.
///
/// Pipeline SMS have a DeliveryLog row keyed by the Twilio SID, which is
/// updated here. Direct sends (phone verification) have no row — for those
/// the structured log entry is the record.
/// </summary>
public sealed record RecordSmsDeliveryStatusCommand(
    string MessageSid,
    string MessageStatus,
    string? ErrorCode) : IRequest<Result>;

public sealed partial class RecordSmsDeliveryStatusCommandHandler(
    NotificationDbContext dbContext,
    ILogger<RecordSmsDeliveryStatusCommandHandler> logger)
    : IRequestHandler<RecordSmsDeliveryStatusCommand, Result>
{
    public async Task<Result> Handle(
        RecordSmsDeliveryStatusCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var status = request.MessageStatus.ToUpperInvariant();
        var isDelivered = status == "DELIVERED";
        var isFailed = status is "UNDELIVERED" or "FAILED";

        if (!isDelivered && !isFailed)
        {
            // Intermediate transitions (queued/sending/sent) — nothing to record.
            return Result.Success();
        }

        if (isFailed)
        {
            LogSmsUndelivered(logger, request.MessageSid, request.MessageStatus, request.ErrorCode);
        }
        else
        {
            LogSmsDelivered(logger, request.MessageSid);
        }

        var deliveryLog = await dbContext.DeliveryLogs
            .FirstOrDefaultAsync(d => d.ProviderMessageId == request.MessageSid, cancellationToken)
            .ConfigureAwait(false);

        if (deliveryLog is null)
        {
            // Direct send (e.g. phone verification) — no pipeline record to update.
            return Result.Success();
        }

        if (isDelivered)
        {
            deliveryLog.MarkDelivered(DateTime.UtcNow);

            var notification = await dbContext.Notifications
                .FirstOrDefaultAsync(n => n.Id == deliveryLog.NotificationId, cancellationToken)
                .ConfigureAwait(false);
            if (notification is { Status: NotificationStatus.Sent })
            {
                notification.MarkDelivered(DateTime.UtcNow);
            }
        }
        else
        {
            var detail = string.IsNullOrWhiteSpace(request.ErrorCode)
                ? request.MessageStatus
                : $"{request.MessageStatus} (Twilio error {request.ErrorCode})";
            deliveryLog.RecordFailure(detail);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "SMS {MessageSid} was not delivered: status {MessageStatus}, Twilio error code {ErrorCode}")]
    private static partial void LogSmsUndelivered(ILogger logger, string messageSid, string messageStatus, string? errorCode);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "SMS {MessageSid} confirmed delivered")]
    private static partial void LogSmsDelivered(ILogger logger, string messageSid);
}
