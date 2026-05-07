using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised when an approved partner endorsement is explicitly revoked by the partner
/// (or a platform admin). Subscribed by VerificationAndRisk to recompute the tenant's
/// risk profile, falling back to the tenant's underlying insurance status when no other
/// active endorsement remains.
/// </summary>
public sealed record PartnerEndorsementRevokedEvent(
    Guid EndorsementId,
    Guid OrganizationId,
    string OrganizationName,
    Guid TenantUserId,
    Guid RevokedByUserId,
    string Reason,
    DateTime RevokedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
