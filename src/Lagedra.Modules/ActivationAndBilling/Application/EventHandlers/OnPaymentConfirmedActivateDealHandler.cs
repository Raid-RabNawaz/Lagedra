using Lagedra.Modules.ActivationAndBilling.Application.Commands;
using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

public sealed partial class OnPaymentConfirmedActivateDealHandler(
    IMediator mediator,
    IAuditTrailWriter auditTrail,
    ILogger<OnPaymentConfirmedActivateDealHandler> logger)
    : IDomainEventHandler<PaymentConfirmedEvent>
{
    public async Task Handle(PaymentConfirmedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogActivating(logger, domainEvent.DealId);

        await auditTrail.RecordAsync(
            null,
            "payment.captured",
            "Deal",
            domainEvent.DealId.ToString(),
            ct: ct).ConfigureAwait(false);

        var result = await mediator.Send(new ActivateDealCommand(domainEvent.DealId), ct)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            LogActivationFailed(logger, domainEvent.DealId, result.Error.Description);
        }
        else
        {
            await auditTrail.RecordAsync(
                null,
                "booking.activated",
                "Deal",
                domainEvent.DealId.ToString(),
                ct: ct).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Payment confirmed, activating deal {DealId}")]
    private static partial void LogActivating(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Failed to activate deal {DealId} after payment confirmation: {Reason}")]
    private static partial void LogActivationFailed(ILogger logger, Guid dealId, string reason);
}
