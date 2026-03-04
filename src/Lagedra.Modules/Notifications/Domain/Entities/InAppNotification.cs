namespace Lagedra.Modules.Notifications.Domain.Entities;

public sealed class InAppNotification
{
    public Guid Id { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public Guid? RelatedEntityId { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private InAppNotification() { }

    public static InAppNotification Create(
        Guid recipientUserId,
        string title,
        string body,
        string category,
        Guid? relatedEntityId = null,
        string? relatedEntityType = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        return new InAppNotification
        {
            Id = Guid.NewGuid(),
            RecipientUserId = recipientUserId,
            Title = title,
            Body = body,
            Category = category,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
