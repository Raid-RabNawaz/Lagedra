using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised when a tenant opens an unusually high number of listing inquiries
/// in a short window. Anti-abuse turns this into a fraud flag for review.
/// </summary>
public sealed record InquiryVolumeAbuseDetectedEvent(
    Guid TenantUserId,
    int InquiryCount24h) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
