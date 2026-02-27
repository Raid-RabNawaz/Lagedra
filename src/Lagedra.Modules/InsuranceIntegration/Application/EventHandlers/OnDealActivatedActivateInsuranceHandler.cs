using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.InsuranceIntegration.Application.Commands;
using Lagedra.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed partial class OnDealActivatedActivateInsuranceHandler(
    ISender sender,
    ILogger<OnDealActivatedActivateInsuranceHandler> logger)
    : IDomainEventHandler<DealActivatedEvent>
{
    public async Task Handle(DealActivatedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogActivatingInsurance(logger, domainEvent.DealId);

        await sender.Send(new RecordInsuranceActiveCommand(
            domainEvent.DealId,
            Provider: null,
            PolicyNumber: null,
            CoverageScope: "Platform-managed",
            ExpiresAt: null), ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Activating insurance for deal {DealId} after deal activation")]
    private static partial void LogActivatingInsurance(ILogger logger, Guid dealId);
}
