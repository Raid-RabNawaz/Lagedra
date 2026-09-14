using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Application.Services;

/// <summary>
/// Resolves a tenant's <see cref="TenantVerificationTier"/> at reservation
/// request time so the predetermined deposit can be selected automatically.
/// Highest tier wins: Blocklisted &gt; Elite &gt; Trusted &gt; Verified &gt; Unverified.
/// </summary>
public interface ITenantVerificationTierResolver
{
    Task<TenantVerificationTierResult> ResolveAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Outcome of tier resolution. <see cref="PartnerOrganizationId"/> is populated
/// only when <see cref="Tier"/> is <see cref="TenantVerificationTier.Elite"/>.
/// </summary>
public sealed record TenantVerificationTierResult(
    TenantVerificationTier Tier,
    Guid? PartnerOrganizationId);
