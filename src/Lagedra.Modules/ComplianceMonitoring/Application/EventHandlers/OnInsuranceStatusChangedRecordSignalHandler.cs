using Lagedra.Modules.ComplianceMonitoring.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;

namespace Lagedra.Modules.ComplianceMonitoring.Application.EventHandlers;

public sealed class OnInsuranceStatusChangedRecordSignalHandler(
    ComplianceMonitoringDbContext dbContext)
    : IDomainEventHandler<InsuranceStatusChangedEvent>
{
    public async Task Handle(InsuranceStatusChangedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        if (domainEvent.NewState != InsuranceState.NotActive)
        {
            return;
        }

        var signal = Domain.Entities.MonitoredComplianceSignal.Record(
            domainEvent.DealId,
            "InsuranceLapse",
            "InsuranceIntegration",
            domainEvent.OccurredAt);

        dbContext.Signals.Add(signal);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
