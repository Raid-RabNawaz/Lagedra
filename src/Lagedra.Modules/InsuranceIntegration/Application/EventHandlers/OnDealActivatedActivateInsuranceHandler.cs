using Lagedra.SharedKernel.Integration.Events;
using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed partial class OnDealActivatedActivateInsuranceHandler(
    InsuranceDbContext insuranceDb,
    IDealApplicationStatusProvider dealApplicationProvider,
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

        var requestedCheckOut = await dealApplicationProvider
            .GetRequestedCheckOutAsync(domainEvent.DealId, ct)
            .ConfigureAwait(false);

        DateTime? expiresAt = requestedCheckOut.HasValue
            ? DateTime.SpecifyKind(
                requestedCheckOut.Value.ToDateTime(TimeOnly.MinValue),
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
