using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.Modules.VerificationAndRisk.Application.Commands;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.VerificationAndRisk.Application.EventHandlers;

public sealed partial class OnIdentityVerifiedRecalculateRiskHandler(
    ISender sender,
    IVerificationSignalProvider signalProvider,
    IUserInsuranceStatusProvider insuranceProvider,
    IUserViolationCountProvider violationProvider,
    ILogger<OnIdentityVerifiedRecalculateRiskHandler> logger)
    : IDomainEventHandler<IdentityVerifiedEvent>
{
    public async Task Handle(IdentityVerifiedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogRecalculating(logger, domainEvent.UserId);

        var signals = await signalProvider.GetSignalsAsync(domainEvent.UserId, ct).ConfigureAwait(false);
        var insurance = await insuranceProvider.GetBestStatusForUserAsync(domainEvent.UserId, ct).ConfigureAwait(false);
        var violationCount = await violationProvider.GetActiveViolationCountAsync(domainEvent.UserId, ct).ConfigureAwait(false);

        await sender.Send(new RecalculateVerificationClassCommand(
            domainEvent.UserId,
            IdentityVerificationStatus.Verified,
            MapBackgroundStatus(signals),
            MapInsuranceStatus(insurance),
            violationCount), ct).ConfigureAwait(false);
    }

    internal static BackgroundCheckStatus MapBackgroundStatus(VerificationSignalDto? signals)
    {
        if (signals is null) return BackgroundCheckStatus.Pending;
        if (signals.IsBackgroundCheckPassed) return BackgroundCheckStatus.Pass;
        if (signals.IsBackgroundCheckFailed) return BackgroundCheckStatus.Fail;
        if (signals.IsBackgroundCheckUnderReview) return BackgroundCheckStatus.Review;
        return BackgroundCheckStatus.Pending;
    }

    internal static InsuranceStatus MapInsuranceStatus(UserInsuranceStatusDto ins)
    {
        if (ins.HasInstitutionBackedPolicy) return InsuranceStatus.InstitutionBacked;
        if (ins.HasActivePolicy) return InsuranceStatus.Active;
        return InsuranceStatus.None;
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Recalculating risk after identity verified for user {UserId}")]
    private static partial void LogRecalculating(ILogger logger, Guid userId);
}
