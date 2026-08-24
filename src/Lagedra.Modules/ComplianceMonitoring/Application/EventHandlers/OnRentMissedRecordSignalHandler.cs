using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;

namespace Lagedra.Modules.ComplianceMonitoring.Application.EventHandlers;

/// <summary>
/// A host reporting unreceived rent (months 2+ are paid off-platform) is a
/// payment-default signal like a stopped subscription: the compliance
/// scanner turns repeated signals into violations and escalations.
/// </summary>
public sealed class OnRentMissedRecordSignalHandler(
    ComplianceMonitoringDbContext dbContext)
    : IDomainEventHandler<RentMissedEvent>
{
    public async Task Handle(RentMissedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var signal = Domain.Entities.MonitoredComplianceSignal.Record(
            domainEvent.DealId,
            "PaymentDefault",
            "ActivationAndBilling",
            domainEvent.OccurredAt);

        dbContext.Signals.Add(signal);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
