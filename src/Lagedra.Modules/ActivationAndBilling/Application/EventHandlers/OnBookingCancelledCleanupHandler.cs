using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

public sealed partial class OnBookingCancelledCleanupHandler(
    IMediator mediator,
    ILogger<OnBookingCancelledCleanupHandler> logger)
    : IDomainEventHandler<BookingCancelledEvent>
{
    public async Task Handle(BookingCancelledEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogCancellation(logger, domainEvent.DealId, domainEvent.Reason, domainEvent.IsAutoCancel);

        await mediator.Publish(domainEvent, ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Booking cancelled for deal {DealId}: {Reason} (auto={IsAutoCancel})")]
    private static partial void LogCancellation(ILogger logger, Guid dealId, string reason, bool isAutoCancel);
}
