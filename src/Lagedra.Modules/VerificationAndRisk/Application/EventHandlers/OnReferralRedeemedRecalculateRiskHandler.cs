using Lagedra.Modules.VerificationAndRisk.Application.Commands;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.VerificationAndRisk.Application.EventHandlers;

/// <summary>
/// Handles a partner referral redemption.
///
/// HISTORICAL BUG (fixed in Phase 18.1): the previous version of this handler hard-coded
/// the redeemer's identity, background-check, and violation signals to "Verified / Pass / 0",
/// which meant any URL click flipped the redeemer to <see cref="VerificationClass.Low"/>
/// regardless of their real KYC state.
///
/// Current behaviour:
///   - Reads the user's real identity + background-check + violation signals.
///   - Only invokes <see cref="RecalculateVerificationClassCommand"/> when the user
///     already has <see cref="IdentityVerificationStatus.Verified"/> AND a background-check
///     status of <see cref="BackgroundCheckStatus.Pass"/> or <see cref="BackgroundCheckStatus.Review"/>.
///   - When eligible, the command is dispatched with the user's REAL signals, and only
///     the insurance status is upgraded to <see cref="InsuranceStatus.InstitutionBacked"/>.
///   - When ineligible, the redemption row in <c>partner.referral_redemptions</c> is still
///     present (written upstream by RedeemReferralLinkCommandHandler), and a structured
///     warning is logged. No risk recompute is performed.
///
/// Going forward, the canonical mechanism for granting <see cref="InsuranceStatus.InstitutionBacked"/>
/// is an approved <c>PartnerEndorsement</c> (Phase 18.5 / 18.6); referral redemption is reduced
/// to channel attribution only.
/// </summary>
public sealed partial class OnReferralRedeemedRecalculateRiskHandler(
    ISender sender,
    IVerificationSignalProvider signalProvider,
    IUserViolationCountProvider violationProvider,
    ILogger<OnReferralRedeemedRecalculateRiskHandler> logger)
    : IDomainEventHandler<ReferralRedeemedEvent>
{
    public async Task Handle(ReferralRedeemedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var signals = await signalProvider
            .GetSignalsAsync(domainEvent.RedeemedByUserId, ct)
            .ConfigureAwait(false);

        var identityStatus = MapIdentityStatus(signals);
        var backgroundStatus = OnIdentityVerifiedRecalculateRiskHandler.MapBackgroundStatus(signals);

        if (!IsEligibleForInstitutionBackedUpgrade(identityStatus, backgroundStatus))
        {
            LogSkippingRecompute(
                logger,
                domainEvent.RedeemedByUserId,
                domainEvent.OrganizationName,
                identityStatus,
                backgroundStatus);
            return;
        }

        var violationCount = await violationProvider
            .GetActiveViolationCountAsync(domainEvent.RedeemedByUserId, ct)
            .ConfigureAwait(false);

        LogRecalculating(logger, domainEvent.RedeemedByUserId, domainEvent.OrganizationName);

        await sender.Send(new RecalculateVerificationClassCommand(
            domainEvent.RedeemedByUserId,
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
        Message = "Recalculating risk for partner-referred user {UserId} from organization '{OrgName}' (eligible for InstitutionBacked upgrade)")]
    private static partial void LogRecalculating(ILogger logger, Guid userId, string orgName);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Skipping risk recompute for partner-referred user {UserId} from organization '{OrgName}': pre-conditions not met (identity={Identity}, background={Background}). Redemption row remains in partner.referral_redemptions for audit.")]
    private static partial void LogSkippingRecompute(
        ILogger logger,
        Guid userId,
        string orgName,
        IdentityVerificationStatus identity,
        BackgroundCheckStatus background);
}
