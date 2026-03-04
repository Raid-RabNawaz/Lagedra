using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ContentManagement.Domain.Events;

public sealed record BlogPostPublishedEvent(
    Guid BlogPostId,
    string Slug,
    string Title,
    DateTime PublishedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
