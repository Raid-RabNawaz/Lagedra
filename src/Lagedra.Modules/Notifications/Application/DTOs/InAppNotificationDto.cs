namespace Lagedra.Modules.Notifications.Application.DTOs;

public sealed record InAppNotificationListDto(
    Guid Id,
    string Title,
    string Body,
    string Category,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    bool IsRead,
    DateTime CreatedAt);
