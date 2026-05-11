using Lagedra.Modules.VerificationAndRisk.Application.Commands;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.VerificationAndRisk.Application.EventHandlers;

/// <summary>
/// Mirror of <see cref="OnPartnerEndorsementRevokedRecalculateRiskHandler"/> for the
/// time-based <see cref="PartnerEndorsementExpiredEvent"/> raised by the
/// <c>ExpirePartnerEndorsementsJob</c>.
/// </summary>
public sealed partial class OnPartnerEndorsementExpiredRecalculateRiskHandler(
    ISender sender,
    IPartnerEndorsementProvider endorsementProvider,
    IUserInsuranceStatusProvider insuranceProvider,
    IVerificationSignalProvider signalProvider,
    IUserViolationCountProvider violationProvider,
    ILogger<OnPartnerEndorsementExpiredRecalculateRiskHandler> logger)
    : IDomainEventHandler<PartnerEndorsementExpiredEvent>
{
    public async Task Handle(PartnerEndorsementExpiredEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var stillEndorsed = await endorsementProvider
            .HasActiveEndorsementAsync(domainEvent.TenantUserId, ct)
            .ConfigureAwait(false);

        if (stillEndorsed)
        {
            LogStillEndorsed(logger, domainEvent.TenantUserId, domainEvent.OrganizationName);
            return;
        }

        var insurance = await insuranceProvider
            .GetBestStatusForUserAsync(domainEvent.TenantUserId, ct)
            .ConfigureAwait(false);

        var signals = await signalProvider
            .GetSignalsAsync(domainEvent.TenantUserId, ct)
            .ConfigureAwait(false);

        var violationCount = await violationProvider
            .GetActiveViolationCountAsync(domainEvent.TenantUserId, ct)
            .ConfigureAwait(false);

        var identityStatus = signals?.IsIdentityVerified == true
            ? IdentityVerificationStatus.Verified
            : signals?.IsIdentityFailed == true
                ? IdentityVerificationStatus.Failed
                : IdentityVerificationStatus.Pending;

        LogRecalculating(logger, domainEvent.TenantUserId, domainEvent.OrganizationName);

        await sender.Send(new RecalculateVerificationClassCommand(
            domainEvent.TenantUserId,
            identityStatus,
            OnIdentityVerifiedRecalculateRiskHandler.MapBackgroundStatus(signals),
            OnIdentityVerifiedRecalculateRiskHandler.MapInsuranceStatus(insurance),
            violationCount), ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Endorsement expired for tenant {TenantId} from '{OrgName}', but other active endorsements remain — no risk recompute needed")]
    private static partial void LogStillEndorsed(ILogger logger, Guid tenantId, string orgName);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Recalculating risk for tenant {TenantId} after endorsement expired from '{OrgName}' (no other active endorsements)")]
    private static partial void LogRecalculating(ILogger logger, Guid tenantId, string orgName);
}
