using Lagedra.Modules.InsuranceIntegration.Application.Services;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed class OnTruthSurfaceConfirmedRequestTruviVerificationHandler(
    TruviScreeningService screening)
    : IDomainEventHandler<TruthSurfaceConfirmedEvent>
{
    public Task Handle(TruthSurfaceConfirmedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return screening.RequestForDealAsync(domainEvent.DealId, ct);
    }
}
