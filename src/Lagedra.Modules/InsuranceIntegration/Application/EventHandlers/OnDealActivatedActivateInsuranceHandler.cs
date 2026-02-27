using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed partial class OnDealActivatedActivateInsuranceHandler(
    InsuranceDbContext insuranceDb,
    BillingDbContext billingDb,
    ILogger<OnDealActivatedActivateInsuranceHandler> logger)
    : IDomainEventHandler<DealActivatedEvent>
{
    public async Task Handle(DealActivatedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogActivatingInsurance(logger, domainEvent.DealId, domainEvent.TenantUserId);

        var record = await insuranceDb.PolicyRecords
            .FirstOrDefaultAsync(r => r.DealId == domainEvent.DealId, ct)
            .ConfigureAwait(false);

        if (record is null)
        {
            record = InsurancePolicyRecord.Create(domainEvent.TenantUserId, domainEvent.DealId);
            insuranceDb.PolicyRecords.Add(record);
        }

        var application = await billingDb.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == domainEvent.DealId, ct)
            .ConfigureAwait(false);

        DateTime? expiresAt = application is not null
            ? DateTime.SpecifyKind(
                application.RequestedCheckOut.ToDateTime(TimeOnly.MinValue),
                DateTimeKind.Utc)
            : null;

        record.RecordActive(
            provider: null,
            policyNumber: null,
            coverageScope: "Platform-managed",
            expiresAt: expiresAt);

        await insuranceDb.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Activating insurance for deal {DealId} (tenant {TenantUserId})")]
    private static partial void LogActivatingInsurance(ILogger logger, Guid dealId, Guid tenantUserId);
}
