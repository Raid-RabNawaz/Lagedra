using Lagedra.SharedKernel.Domain;

namespace Lagedra.Modules.StructuredInquiry.Domain.Events;

/// <summary>
/// Phase 17 — raised when a tenant opens a brand-new pre-booking inquiry
/// thread on a listing. Picked up by the notification module to email the
/// host (template <c>inquiry_started</c>).
/// </summary>
public sealed record ListingInquiryStartedEvent(
    Guid SessionId,
    Guid ListingId,
    Guid LandlordUserId,
    Guid TenantUserId,
    DateTime StartedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = StartedAt;
}
