using Lagedra.Modules.ActivationAndBilling.Application.Services;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Settings;
using Lagedra.SharedKernel.Time;
using Microsoft.Extensions.Options;

namespace Lagedra.Modules.ActivationAndBilling.Infrastructure.Services;

/// <summary>
/// Gathers cross-module signals and classifies via <see cref="TrustTiersPolicy"/>.
/// </summary>
public sealed class TenantVerificationTierResolver(
    IPartnerEndorsementProvider partnerEndorsementProvider,
    IVerificationSignalProvider verificationSignalProvider,
    ICompletedStayCountProvider completedStayCountProvider,
    IHostToGuestReputationProvider hostToGuestReputationProvider,
    IUserViolationCountProvider violationCountProvider,
    IArbitrationStandingProvider arbitrationStandingProvider,
    IBookingRestrictionProvider bookingRestrictionProvider,
    IOptions<TrustTiersOptions> options,
    IClock clock)
    : ITenantVerificationTierResolver
{
    public async Task<TenantVerificationTierResult> ResolveAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default)
    {
        var isRestricted = await bookingRestrictionProvider
            .IsBlockedFromBookingAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);
        var isPlatformBanned = await arbitrationStandingProvider
            .HasPlatformBanAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);
        var isBlocked = isRestricted || isPlatformBanned;

        var endorsements = await partnerEndorsementProvider
            .GetActiveEndorsementsAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);

        Guid? partnerOrganizationId = endorsements.Count > 0
            ? endorsements.OrderBy(e => e.OrganizationId).First().OrganizationId
            : null;

        var signals = await verificationSignalProvider
            .GetSignalsAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var stays = await completedStayCountProvider
            .GetClosedStayCountAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var reviews = await hostToGuestReputationProvider
            .GetAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var violations = await violationCountProvider
            .GetActiveViolationCountAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var unresolvedArbitration = await arbitrationStandingProvider
            .HasUnresolvedCaseAsync(tenantUserId, cancellationToken)
            .ConfigureAwait(false);

        var opts = options.Value;
        var hasRecentPass = signals is { IsBackgroundCheckPassed: true, LastScreeningAt: { } at }
            && at > clock.UtcNow.AddDays(-opts.ScreeningFactorTtlDays);

        var tier = TrustTiersPolicy.Classify(
            new TrustTierSignals(
                IsIdentityVerified: signals?.IsIdentityVerified == true,
                IsBlocklisted: isBlocked,
                HasActivePartnerEndorsement: endorsements.Count > 0,
                CompletedStayCount: stays,
                HostToGuestReviewCount: reviews.ReviewCount,
                HostToGuestAverage: reviews.AverageOverall,
                OpenViolationCount: violations,
                HasUnresolvedArbitration: unresolvedArbitration,
                HasRecentScreeningPass: hasRecentPass),
            opts);

        return new TenantVerificationTierResult(
            tier,
            tier == TenantVerificationTier.Elite ? partnerOrganizationId : null);
    }
}
