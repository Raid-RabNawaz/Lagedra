using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed partial class OnDealActivatedActivateInsuranceHandler(
    InsuranceDbContext insuranceDb,
    ILogger<OnDealActivatedActivateInsuranceHandler> logger)
    : IDomainEventHandler<DealActivatedEvent>
{
    public async Task Handle(DealActivatedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var record = await insuranceDb.PolicyRecords
            .FirstOrDefaultAsync(r => r.DealId == domainEvent.DealId, ct)
            .ConfigureAwait(false);

        if (record is not null)
        {
            LogAlreadyPresent(logger, domainEvent.DealId, record.ScreeningStatus);
            return;
        }

        insuranceDb.PolicyRecords.Add(
            InsurancePolicyRecord.Create(domainEvent.TenantUserId, domainEvent.DealId));
        await insuranceDb.SaveChangesAsync(ct).ConfigureAwait(false);
        LogRecordCreated(logger, domainEvent.DealId);
    }

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "Insurance record already exists for deal {DealId} (screening {ScreeningStatus}); not overwriting")]
    private static partial void LogAlreadyPresent(ILogger logger, Guid dealId, string? screeningStatus);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Created empty insurance record for deal {DealId} because screening never ran")]
    private static partial void LogRecordCreated(ILogger logger, Guid dealId);
}
