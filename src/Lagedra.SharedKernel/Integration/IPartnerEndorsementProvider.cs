namespace Lagedra.SharedKernel.Integration;

/// <summary>
/// Cross-module integration interface implemented by the PartnerNetwork module.
/// Exposes the minimum data the VerificationAndRisk + TruthSurface modules need to
/// reason about a tenant's partner-endorsement status without taking a direct
/// DbContext dependency.
/// </summary>
public interface IPartnerEndorsementProvider
{
    /// <summary>
    /// Returns <c>true</c> iff the tenant has at least one currently-active
    /// (Approved AND not yet expired) endorsement from any verified partner.
    /// </summary>
    Task<bool> HasActiveEndorsementAsync(Guid tenantUserId, CancellationToken ct = default);

    /// <summary>
    /// Returns the active (Approved + not expired) endorsements for the tenant.
    /// Each entry includes the partner's display name and approval window so the
    /// risk view + Truth Surface canonical content can render attribution honestly.
    /// </summary>
    Task<IReadOnlyList<ActiveEndorsementInfo>> GetActiveEndorsementsAsync(
        Guid tenantUserId,
        CancellationToken ct = default);
}

public sealed record ActiveEndorsementInfo(
    Guid EndorsementId,
    Guid OrganizationId,
    string OrganizationName,
    DateTime ApprovedAt,
    DateTime ExpiresAt);
