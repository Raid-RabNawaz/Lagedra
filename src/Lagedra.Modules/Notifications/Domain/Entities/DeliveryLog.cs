using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Notifications.Domain.Entities;

public sealed class DeliveryLog : Entity<Guid>
{
    public Guid NotificationId { get; private set; }
    public string? BrevoMessageId { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public string? Error { get; private set; }

    private DeliveryLog() { }

    public DeliveryLog(Guid notificationId, string? brevoMessageId, DateTime? deliveredAt, string? error)
        : base(Guid.NewGuid())
    {
        NotificationId = notificationId;
        BrevoMessageId = brevoMessageId;
        DeliveredAt = deliveredAt;
        Error = error;
    }
}
