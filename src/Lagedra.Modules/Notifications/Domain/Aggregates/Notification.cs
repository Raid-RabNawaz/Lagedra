using Lagedra.Modules.Notifications.Domain.Enums;
using Lagedra.Modules.Notifications.Domain.Events;
using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Notifications.Domain.Aggregates;

public sealed class Notification : AggregateRoot<Guid>
{
    public Guid RecipientUserId { get; private set; }
    public string RecipientEmail { get; private set; } = string.Empty;
    public NotificationChannel Channel { get; private set; }
    public string TemplateId { get; private set; } = string.Empty;
    public NotificationStatus Status { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public Dictionary<string, string> Payload { get; private set; } = [];
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }

    private Notification() { }

    public static Notification Queue(
        Guid recipientUserId,
        string recipientEmail,
        NotificationChannel channel,
        string templateId,
        Dictionary<string, string> payload,
        DateTime? scheduledAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateId);
        ArgumentNullException.ThrowIfNull(payload);

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            RecipientEmail = recipientEmail,
            Channel = channel,
            TemplateId = templateId,
            Status = NotificationStatus.Queued,
            ScheduledAt = scheduledAt ?? DateTime.UtcNow,
            Payload = payload
        };

        notification.AddDomainEvent(new NotificationQueuedEvent(
            notification.Id, recipientUserId, templateId));

        return notification;
    }

    public void MarkSent(DateTime sentAt)
    {
        if (Status is not (NotificationStatus.Queued or NotificationStatus.Failed))
        {
            throw new InvalidOperationException($"Cannot mark notification as sent in status '{Status}'.");
        }

        Status = NotificationStatus.Sent;
        SentAt = sentAt;
        AttemptCount++;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        if (Status is not (NotificationStatus.Queued or NotificationStatus.Failed))
        {
            throw new InvalidOperationException($"Cannot mark notification as failed in status '{Status}'.");
        }

        Status = NotificationStatus.Failed;
        AttemptCount++;
        LastError = error;

        AddDomainEvent(new NotificationFailedEvent(Id, error));
    }

    public void MarkDelivered(DateTime deliveredAt)
    {
        if (Status != NotificationStatus.Sent)
        {
            throw new InvalidOperationException($"Cannot mark notification as delivered in status '{Status}'.");
        }

        Status = NotificationStatus.Delivered;

        AddDomainEvent(new NotificationDeliveredEvent(Id, deliveredAt));
    }
}
