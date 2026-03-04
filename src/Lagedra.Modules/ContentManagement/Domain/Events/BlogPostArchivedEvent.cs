using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.ContentManagement.Domain.Events;

public sealed record BlogPostArchivedEvent(
    Guid BlogPostId,
    string Slug) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
