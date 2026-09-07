using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed class OnBookingCancelledCancelInsuranceHandler(
    TruviScreeningService screening)
    : IDomainEventHandler<BookingCancelledEvent>
{
    public Task Handle(BookingCancelledEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return screening.CancelForDealAsync(
            domainEvent.DealId,
            $"Booking cancelled: {domainEvent.Reason}",
            ct);
    }
}
