using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;

namespace Lagedra.Modules.ComplianceMonitoring.Application.EventHandlers;

public sealed class OnBillingStoppedRecordSignalHandler(
    ComplianceMonitoringDbContext dbContext)
    : IDomainEventHandler<BillingStoppedEvent>
{
    public async Task Handle(BillingStoppedEvent domainEvent, CancellationToken ct = default)
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
