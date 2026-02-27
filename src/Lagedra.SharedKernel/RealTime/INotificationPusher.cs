namespace Lagedra.SharedKernel.RealTime;

public sealed record InAppNotificationDto(
    Guid Id,
    string Title,
    string Body,
    string Category,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    DateTime CreatedAt);

public interface INotificationPusher
{
    Task PushToUserAsync(Guid userId, InAppNotificationDto notification, CancellationToken ct = default);
    Task PushToUsersAsync(IEnumerable<Guid> userIds, InAppNotificationDto notification, CancellationToken ct = default);
}
