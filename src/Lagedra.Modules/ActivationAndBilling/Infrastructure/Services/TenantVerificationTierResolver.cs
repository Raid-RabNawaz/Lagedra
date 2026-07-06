using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;

/// <summary>
/// Computes the tenant's verification tier from the cross-module providers.
///
/// Rules (highest wins):
///   1. PartnerGuaranteed — at least one active (Approved + not expired)
///      partner endorsement.
///   2. BackgroundVerified — identity verified AND background check passed.
///      A background result still "under review" does NOT count.
///   3. Unverified — anything else (no profile, pending, failed, review-only).
/// </summary>
public sealed class TenantVerificationTierResolver(
    IPartnerEndorsementProvider partnerEndorsementProvider,
    IVerificationSignalProvider verificationSignalProvider)
    : ITenantVerificationTierResolver
{
    public async Task<TenantVerificationTierResult> ResolveAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        var endorsements = await partnerEndorsementProvider
            .GetActiveEndorsementsAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);

        if (endorsements.Count > 0)
        {
            // Deterministic pick (lowest org id) mirrors how the Truth Surface
            // canonical content orders endorsements, so the partner attributed
            // on the booking matches the one rendered on the agreement.
            var organizationId = endorsements
                .OrderBy(e => e.OrganizationId)
                .First()
                .OrganizationId;

            return new TenantVerificationTierResult(
                TenantVerificationTier.PartnerGuaranteed,
                organizationId);
        }

        var signals = await verificationSignalProvider
            .GetSignalsAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);

        if (signals is { IsIdentityVerified: true, IsBackgroundCheckPassed: true })
        {
            return new TenantVerificationTierResult(
                TenantVerificationTier.BackgroundVerified,
                null);
        }

        return new TenantVerificationTierResult(TenantVerificationTier.Unverified, null);
    }
}
