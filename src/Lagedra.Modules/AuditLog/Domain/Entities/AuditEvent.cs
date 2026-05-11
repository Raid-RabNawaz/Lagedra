using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.AuditLog.Domain.Entities;

public sealed class AuditEvent : Entity<Guid>
{
    public Guid? UserId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime Timestamp { get; private set; }

    private AuditEvent() { }

    public static AuditEvent Create(
        Guid? userId,
        string eventType,
        string entityType,
        string entityId,
        string? details,
        string? ipAddress)
    {
        return new AuditEvent
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow,
        };
    }
}
