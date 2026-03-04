using Lagedra.Modules.InsuranceIntegration.Domain.Enums;
using Lagedra.Modules.InsuranceIntegration.Domain.Events;
using Lagedra.Modules.VerificationAndRisk.Application.Commands;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.VerificationAndRisk.Application.EventHandlers;

public sealed partial class OnInsuranceStatusChangedRecalculateRiskHandler(
    ISender sender,
    IUserInsuranceStatusProvider insuranceProvider,
    IVerificationSignalProvider signalProvider,
    IUserViolationCountProvider violationProvider,
    ILogger<OnInsuranceStatusChangedRecalculateRiskHandler> logger)
    : IDomainEventHandler<InsuranceStatusChangedEvent>
{
    public async Task Handle(InsuranceStatusChangedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var tenantUserId = await insuranceProvider
            .GetTenantUserIdForDealAsync(domainEvent.DealId, ct)
            .ConfigureAwait(false);

        if (tenantUserId is null)
        {
            LogTenantNotFound(logger, domainEvent.DealId);
            return;
        }

        LogRecalculating(logger, tenantUserId.Value, domainEvent.DealId);

        var signals = await signalProvider.GetSignalsAsync(tenantUserId.Value, ct).ConfigureAwait(false);
        var violationCount = await violationProvider.GetActiveViolationCountAsync(tenantUserId.Value, ct).ConfigureAwait(false);

        var identityStatus = signals?.IsIdentityVerified == true
            ? IdentityVerificationStatus.Verified
            : signals?.IsIdentityFailed == true
                ? IdentityVerificationStatus.Failed
                : IdentityVerificationStatus.Pending;

        var insuranceStatus = domainEvent.NewState switch
        {
            InsuranceState.Active => InsuranceStatus.Active,
            InsuranceState.InstitutionBacked => InsuranceStatus.InstitutionBacked,
            InsuranceState.NotActive => InsuranceStatus.Inactive,
            _ => InsuranceStatus.None
        };

        await sender.Send(new RecalculateVerificationClassCommand(
            tenantUserId.Value,
            identityStatus,
            OnIdentityVerifiedRecalculateRiskHandler.MapBackgroundStatus(signals),
            insuranceStatus,
            violationCount), ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Recalculating risk after insurance status changed for tenant {TenantUserId} on deal {DealId}")]
    private static partial void LogRecalculating(ILogger logger, Guid tenantUserId, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "No tenant found for deal {DealId} — skipping risk recalculation")]
    private static partial void LogTenantNotFound(ILogger logger, Guid dealId);
}
