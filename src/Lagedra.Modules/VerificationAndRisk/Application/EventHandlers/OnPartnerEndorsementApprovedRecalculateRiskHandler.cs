using Lagedra.Modules.VerificationAndRisk.Application.Commands;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.VerificationAndRisk.Application.EventHandlers;

/// <summary>
/// Handles a partner endorsement being approved.
///
/// Mirrors the 18.1 hotfix discipline: this handler reads the tenant's REAL
/// identity / background / violation signals via the existing providers and only
/// upgrades insurance to <see cref="InsuranceStatus.InstitutionBacked"/> when the
/// tenant has already cleared identity (<see cref="IdentityVerificationStatus.Verified"/>)
/// and background-check (<see cref="BackgroundCheckStatus.Pass"/> or
/// <see cref="BackgroundCheckStatus.Review"/>). An endorsement is a partner-attested
/// relationship — it is NOT a substitute for identity / background verification.
/// </summary>
public sealed partial class OnPartnerEndorsementApprovedRecalculateRiskHandler(
    ISender sender,
    IVerificationSignalProvider signalProvider,
    IUserViolationCountProvider violationProvider,
    ILogger<OnPartnerEndorsementApprovedRecalculateRiskHandler> logger)
    : IDomainEventHandler<PartnerEndorsementApprovedEvent>
{
    public async Task Handle(PartnerEndorsementApprovedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var signals = await signalProvider
            .GetSignalsAsync(domainEvent.TenantUserId, ct)
            .ConfigureAwait(false);

        var identityStatus = MapIdentityStatus(signals);
        var backgroundStatus = OnIdentityVerifiedRecalculateRiskHandler.MapBackgroundStatus(signals);

        if (!IsEligibleForInstitutionBackedUpgrade(identityStatus, backgroundStatus))
        {
            LogSkippingRecompute(
                logger,
                domainEvent.TenantUserId,
                domainEvent.OrganizationName,
                identityStatus,
                backgroundStatus);
            return;
        }

        var violationCount = await violationProvider
            .GetActiveViolationCountAsync(domainEvent.TenantUserId, ct)
            .ConfigureAwait(false);

        LogRecalculating(logger, domainEvent.TenantUserId, domainEvent.OrganizationName);

        await sender.Send(new RecalculateVerificationClassCommand(
            domainEvent.TenantUserId,
            identityStatus,
            backgroundStatus,
            InsuranceStatus.InstitutionBacked,
            violationCount), ct).ConfigureAwait(false);
    }

    private static bool IsEligibleForInstitutionBackedUpgrade(
        IdentityVerificationStatus identity,
        BackgroundCheckStatus background) =>
        identity == IdentityVerificationStatus.Verified
        && background is BackgroundCheckStatus.Pass or BackgroundCheckStatus.Review;

    private static IdentityVerificationStatus MapIdentityStatus(VerificationSignalDto? signals)
    {
        if (signals is null) return IdentityVerificationStatus.Pending;
        if (signals.IsIdentityVerified) return IdentityVerificationStatus.Verified;
        if (signals.IsIdentityFailed) return IdentityVerificationStatus.Failed;
        return IdentityVerificationStatus.Pending;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Recalculating risk for endorsed tenant {TenantId} from organization '{OrgName}' (eligible for InstitutionBacked upgrade)")]
    private static partial void LogRecalculating(ILogger logger, Guid tenantId, string orgName);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Skipping risk recompute for endorsed tenant {TenantId} from organization '{OrgName}': identity={Identity}, background={Background} — tenant must complete identity + background-check before partner endorsement upgrades their insurance tier.")]
    private static partial void LogSkippingRecompute(
        ILogger logger,
        Guid tenantId,
        string orgName,
        IdentityVerificationStatus identity,
        BackgroundCheckStatus background);
}
