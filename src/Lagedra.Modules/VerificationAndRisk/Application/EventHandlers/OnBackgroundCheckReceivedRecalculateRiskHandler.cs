using Lagedra.Modules.IdentityAndVerification.Domain.Enums;
using Lagedra.Modules.IdentityAndVerification.Domain.Events;
using Lagedra.Modules.VerificationAndRisk.Application.Commands;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.VerificationAndRisk.Application.EventHandlers;

public sealed partial class OnBackgroundCheckReceivedRecalculateRiskHandler(
    ISender sender,
    IVerificationSignalProvider signalProvider,
    IUserInsuranceStatusProvider insuranceProvider,
    IUserViolationCountProvider violationProvider,
    ILogger<OnBackgroundCheckReceivedRecalculateRiskHandler> logger)
    : IDomainEventHandler<BackgroundCheckReceivedEvent>
{
    public async Task Handle(BackgroundCheckReceivedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogRecalculating(logger, domainEvent.UserId, domainEvent.Result);

        var signals = await signalProvider.GetSignalsAsync(domainEvent.UserId, ct).ConfigureAwait(false);
        var insurance = await insuranceProvider.GetBestStatusForUserAsync(domainEvent.UserId, ct).ConfigureAwait(false);
        var violationCount = await violationProvider.GetActiveViolationCountAsync(domainEvent.UserId, ct).ConfigureAwait(false);

        var identityStatus = signals?.IsIdentityVerified == true
            ? IdentityVerificationStatus.Verified
            : signals?.IsIdentityFailed == true
                ? IdentityVerificationStatus.Failed
                : IdentityVerificationStatus.Pending;

        var bgStatus = domainEvent.Result switch
        {
            BackgroundCheckResult.Pass => BackgroundCheckStatus.Pass,
            BackgroundCheckResult.Fail => BackgroundCheckStatus.Fail,
            BackgroundCheckResult.Review => BackgroundCheckStatus.Review,
            _ => BackgroundCheckStatus.Pending
        };

        await sender.Send(new RecalculateVerificationClassCommand(
            domainEvent.UserId,
            identityStatus,
            bgStatus,
            OnIdentityVerifiedRecalculateRiskHandler.MapInsuranceStatus(insurance),
            violationCount), ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Recalculating risk after background check received for user {UserId}, result: {Result}")]
    private static partial void LogRecalculating(ILogger logger, Guid userId, BackgroundCheckResult result);
}
