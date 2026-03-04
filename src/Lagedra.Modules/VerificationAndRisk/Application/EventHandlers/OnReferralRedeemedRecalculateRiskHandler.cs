using Lagedra.Modules.PartnerNetwork.Domain.Events;
using Lagedra.Modules.VerificationAndRisk.Application.Commands;
using Lagedra.Modules.VerificationAndRisk.Domain.Enums;
using Lagedra.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.VerificationAndRisk.Application.EventHandlers;

public sealed partial class OnReferralRedeemedRecalculateRiskHandler(
    ISender sender,
    ILogger<OnReferralRedeemedRecalculateRiskHandler> logger)
    : IDomainEventHandler<ReferralRedeemedEvent>
{
    public async Task Handle(ReferralRedeemedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogRecalculating(logger, domainEvent.RedeemedByUserId, domainEvent.OrganizationName);

        await sender.Send(new RecalculateVerificationClassCommand(
            domainEvent.RedeemedByUserId,
            IdentityVerificationStatus.Verified,
            BackgroundCheckStatus.Pass,
            InsuranceStatus.InstitutionBacked,
            ViolationCount: 0), ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Recalculating risk for partner-referred user {UserId} from organization '{OrgName}'")]
    private static partial void LogRecalculating(ILogger logger, Guid userId, string orgName);
}
