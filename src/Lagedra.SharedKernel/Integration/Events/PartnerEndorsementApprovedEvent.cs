using Lagedra.SharedKernel.Domain;

namespace Lagedra.SharedKernel.Integration.Events;

/// <summary>
/// Raised when a partner organization approves a tenant endorsement.
/// Subscribed by VerificationAndRisk to upgrade the tenant to <c>InsuranceStatus.InstitutionBacked</c>
/// (subject to the existing identity / background pre-conditions in <c>RecalculateVerificationClassCommand</c>).
/// </summary>
public sealed record PartnerEndorsementApprovedEvent(
    Guid EndorsementId,
    Guid OrganizationId,
    string OrganizationName,
    Guid TenantUserId,
    Guid ApprovedByUserId,
    DateTime ApprovedAt,
    DateTime ExpiresAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
