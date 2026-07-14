using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.Reviews.Domain.Events;

public sealed record PartnerServiceReviewSubmittedEvent(
    Guid ReviewId,
    Guid OrganizationId,
    Guid ReviewerUserId,
    int OverallRating,
    DateTime SubmittedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
