using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised when an approved partner endorsement passes its <c>ExpiresAt</c> deadline.
/// Emitted by the <c>ExpirePartnerEndorsementsJob</c> Quartz job.
/// </summary>
public sealed record PartnerEndorsementExpiredEvent(
    Guid EndorsementId,
    Guid OrganizationId,
    string OrganizationName,
    Guid TenantUserId,
    DateTime ExpiredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
