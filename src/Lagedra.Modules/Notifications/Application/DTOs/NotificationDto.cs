using Lagedra.Modules.Notifications.Domain.Enums;

namespace Lagedra.Modules.Notifications.Application.DTOs;

public sealed record NotificationDto(
    Guid Id,
    Guid RecipientUserId,
    string RecipientEmail,
    NotificationChannel Channel,
    string TemplateId,
    NotificationStatus Status,
    DateTime ScheduledAt,
    DateTime? SentAt,
    int AttemptCount);
