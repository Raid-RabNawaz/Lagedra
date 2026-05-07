using Lagedra.Modules.VerificationAndRisk.Application.Commands;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.VerificationAndRisk.Application.EventHandlers;

/// <summary>
/// Handles a partner endorsement being revoked. Recomputes the tenant's risk profile
/// using the tenant's REAL insurance status — but only if no other active endorsement
/// remains. This avoids dropping the InstitutionBacked tier just because one of multiple
/// partner relationships ended.
/// </summary>
public sealed partial class OnPartnerEndorsementRevokedRecalculateRiskHandler(
    ISender sender,
    IPartnerEndorsementProvider endorsementProvider,
    IUserInsuranceStatusProvider insuranceProvider,
    IVerificationSignalProvider signalProvider,
    IUserViolationCountProvider violationProvider,
    ILogger<OnPartnerEndorsementRevokedRecalculateRiskHandler> logger)
    : IDomainEventHandler<PartnerEndorsementRevokedEvent>
{
    public async Task Handle(PartnerEndorsementRevokedEvent domainEvent, CancellationToken ct = default)
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
        Message = "Endorsement revoked for tenant {TenantId} by '{OrgName}', but other active endorsements remain — no risk recompute needed")]
    private static partial void LogStillEndorsed(ILogger logger, Guid tenantId, string orgName);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Recalculating risk for tenant {TenantId} after endorsement revoked by '{OrgName}' (no other active endorsements)")]
    private static partial void LogRecalculating(ILogger logger, Guid tenantId, string orgName);
}
