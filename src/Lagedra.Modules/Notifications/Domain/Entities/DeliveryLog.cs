using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Notifications.Domain.Entities;

public sealed class DeliveryLog : Entity<Guid>
{
    public Guid NotificationId { get; private set; }

    /// <summary>Provider message id (SendGrid message id, Twilio SID, etc.).</summary>
    public string? ProviderMessageId { get; private set; }

    public DateTime? DeliveredAt { get; private set; }
    public string? Error { get; private set; }

    private DeliveryLog() { }

    public DeliveryLog(Guid notificationId, string? providerMessageId, DateTime? deliveredAt, string? error)
        : base(Guid.NewGuid())
    {
        NotificationId = notificationId;
        ProviderMessageId = providerMessageId;
        DeliveredAt = deliveredAt;
        Error = error;
    }
}
