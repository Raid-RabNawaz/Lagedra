using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;

namespace Lagedra.Modules.ActivationAndBilling.Application.EventHandlers;

/// <summary>
/// Once a deal's payment is confirmed, hand it to the channel publisher so the
/// paid booking can be pushed back to the external PMS / channel the listing
/// came from (merchant-of-record model). The publisher is a no-op for listings
/// that did not originate from a channel and isolates its own failures, so this
/// handler never disrupts payment confirmation or deal activation.
/// </summary>
public sealed class OnPaymentConfirmedPublishToChannelHandler(
    IChannelBookingPublisher channelBookingPublisher)
    : IDomainEventHandler<PaymentConfirmedEvent>
{
    public async Task Handle(PaymentConfirmedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await channelBookingPublisher
            .PublishConfirmedBookingAsync(domainEvent.DealId, ct)
            .ConfigureAwait(false);
    }
}
