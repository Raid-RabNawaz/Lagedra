using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Domain.Events;
using Lagedra.SharedKernel.Domain;
using Lagedra.SharedKernel.Integration.Events;
using Lagedra.SharedKernel.Time;

namespace Lagedra.Modules.PartnerNetwork.Domain.Aggregates;

/// <summary>
/// A partner organization's attested claim ("yes, this person is one of ours") about
/// a tenant. The only mechanism going forward (post-Phase 18) for a tenant to be flagged
/// as <c>InsuranceStatus.InstitutionBacked</c>.
///
/// Lifecycle: see <see cref="PartnerEndorsementStatus"/>. Each transition raises a
/// domain event so the audit pipeline + risk recalculation handlers can react.
///
/// Uniqueness: at most one row per <c>(OrganizationId, TenantUserId)</c> may be in
/// <see cref="PartnerEndorsementStatus.Requested"/> or <see cref="PartnerEndorsementStatus.Approved"/>
/// (enforced by a partial unique index in the EF configuration). Terminal rows (Revoked / Expired)
/// remain for history.
/// </summary>
public sealed class PartnerEndorsement : AggregateRoot<Guid>
{
    public const int DefaultExpirationMonths = 12;

    public Guid OrganizationId { get; private set; }
    public Guid TenantUserId { get; private set; }
    public PartnerEndorsementStatus Status { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public Guid RequestedByUserId { get; private set; }

    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }

    public DateTime? RevokedAt { get; private set; }
    public Guid? RevokedByUserId { get; private set; }
    public string? RevokeReason { get; private set; }

    public DateTime? ExpiresAt { get; private set; }

    public string? Note { get; private set; }

    private PartnerEndorsement() { }

    public static PartnerEndorsement Request(
        Guid organizationId,
        Guid tenantUserId,
        Guid requestedByUserId,
        string? note,
        IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        var endorsement = new PartnerEndorsement
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            TenantUserId = tenantUserId,
            Status = PartnerEndorsementStatus.Requested,
            RequestedAt = now,
            RequestedByUserId = requestedByUserId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        endorsement.AddDomainEvent(new PartnerEndorsementRequestedEvent(
            endorsement.Id,
            organizationId,
            tenantUserId,
            requestedByUserId,
            now));

        return endorsement;
    }

    /// <summary>
    /// Auto-approved factory used by partner-driven invite flows where the partner
    /// itself creates the user account (Phase 18.4). Skips the <see cref="PartnerEndorsementStatus.Requested"/>
    /// state and emits both Requested and Approved events for audit completeness.
    /// </summary>
    public static PartnerEndorsement RequestAndApprove(
        Guid organizationId,
        string organizationName,
        Guid tenantUserId,
        Guid approvedByUserId,
        string? note,
        IClock clock)
    {
        var endorsement = Request(organizationId, tenantUserId, approvedByUserId, note, clock);
        endorsement.Approve(organizationName, approvedByUserId, note: null, clock);
        return endorsement;
    }

    public void Approve(string organizationName, Guid approvedByUserId, string? note, IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationName);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != PartnerEndorsementStatus.Requested)
        {
            throw new InvalidOperationException(
                $"Cannot approve endorsement in status '{Status}'.");
        }

        var now = clock.UtcNow;
        Status = PartnerEndorsementStatus.Approved;
        ApprovedAt = now;
        ApprovedByUserId = approvedByUserId;
        ExpiresAt = now.AddMonths(DefaultExpirationMonths);
        if (!string.IsNullOrWhiteSpace(note))
        {
            Note = note.Trim();
        }
        UpdatedAt = now;

        AddDomainEvent(new PartnerEndorsementApprovedEvent(
            Id,
            OrganizationId,
            organizationName,
            TenantUserId,
            approvedByUserId,
            now,
            ExpiresAt.Value));
    }

    public void Revoke(string organizationName, Guid revokedByUserId, string reason, IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationName);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status is PartnerEndorsementStatus.Revoked or PartnerEndorsementStatus.Expired)
        {
            throw new InvalidOperationException(
                $"Cannot revoke endorsement in terminal status '{Status}'.");
        }

        var now = clock.UtcNow;
        Status = PartnerEndorsementStatus.Revoked;
        RevokedAt = now;
        RevokedByUserId = revokedByUserId;
        RevokeReason = reason.Trim();
        UpdatedAt = now;

        AddDomainEvent(new PartnerEndorsementRevokedEvent(
            Id,
            OrganizationId,
            organizationName,
            TenantUserId,
            revokedByUserId,
            RevokeReason,
            now));
    }

    public void Expire(string organizationName, IClock clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationName);
        ArgumentNullException.ThrowIfNull(clock);

        if (Status != PartnerEndorsementStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Cannot expire endorsement in status '{Status}'.");
        }

        var now = clock.UtcNow;
        Status = PartnerEndorsementStatus.Expired;
        UpdatedAt = now;

        AddDomainEvent(new PartnerEndorsementExpiredEvent(
            Id,
            OrganizationId,
            organizationName,
            TenantUserId,
            now));
    }

    public bool IsActive =>
        Status is PartnerEndorsementStatus.Requested or PartnerEndorsementStatus.Approved;
}
