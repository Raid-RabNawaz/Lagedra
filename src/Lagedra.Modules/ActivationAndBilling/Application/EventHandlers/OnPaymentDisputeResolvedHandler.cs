using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

public sealed partial class OnPaymentDisputeResolvedHandler(
    IMediator mediator,
    ILogger<OnPaymentDisputeResolvedHandler> logger)
    : IDomainEventHandler<PaymentDisputeResolvedEvent>
{
    public async Task Handle(PaymentDisputeResolvedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        if (domainEvent.PaymentValid)
        {
            LogActivating(logger, domainEvent.DealId);

            var result = await mediator.Send(new ActivateDealCommand(domainEvent.DealId), ct)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                LogActivationFailed(logger, domainEvent.DealId, result.Error.Description);
            }
        }
        else
        {
            LogPaymentRejected(logger, domainEvent.DealId);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Dispute resolved as valid, activating deal {DealId}")]
    private static partial void LogActivating(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to activate deal {DealId} after dispute resolution: {Reason}")]
    private static partial void LogActivationFailed(ILogger logger, Guid dealId, string reason);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Dispute resolved as invalid for deal {DealId}, payment rejected")]
    private static partial void LogPaymentRejected(ILogger logger, Guid dealId);
}
