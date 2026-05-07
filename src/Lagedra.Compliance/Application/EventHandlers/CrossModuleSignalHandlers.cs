using Lagedra.Compliance.Domain;
using Lagedra.Compliance.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;

namespace Lagedra.Compliance.Application.EventHandlers;

public sealed class OnInsuranceStatusChangedCreateSignalHandler(
    ComplianceDbContext dbContext)
    : IDomainEventHandler<InsuranceStatusChangedEvent>
{
    public async Task Handle(InsuranceStatusChangedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        if (domainEvent.NewState != SharedKernel.Integration.InsuranceState.NotActive)
        {
            return;
        }

        var signal = ComplianceSignal.Create(
            domainEvent.DealId,
            "InsuranceLapse",
            $"Insurance transitioned from {domainEvent.OldState} to {domainEvent.NewState}");

        dbContext.Signals.Add(signal);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class OnBillingStoppedCreateSignalHandler(
    ComplianceDbContext dbContext)
    : IDomainEventHandler<BillingStoppedEvent>
{
    public async Task Handle(BillingStoppedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var signal = ComplianceSignal.Create(
            domainEvent.DealId,
            "PaymentDefault",
            $"Billing stopped for account {domainEvent.BillingAccountId}");

        dbContext.Signals.Add(signal);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class OnDealCompletedCreateSignalHandler(
    ComplianceDbContext dbContext)
    : IDomainEventHandler<DealCompletedEvent>
{
    public async Task Handle(DealCompletedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var signal = ComplianceSignal.Create(
            domainEvent.DealId,
            "DealCompleted",
            $"Deal completed successfully for billing account {domainEvent.BillingAccountId}");

        dbContext.Signals.Add(signal);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
