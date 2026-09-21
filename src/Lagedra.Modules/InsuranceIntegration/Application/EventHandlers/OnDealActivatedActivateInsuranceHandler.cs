using Lagedra.Modules.InsuranceIntegration.Domain.Aggregates;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Integration.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed partial class OnDealActivatedActivateInsuranceHandler(
    InsuranceDbContext insuranceDb,
    IDealApplicationStatusProvider deals,
    IListingProvider listings,
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

        var deal = await deals.GetDealDetailsAsync(domainEvent.DealId, ct).ConfigureAwait(false);
        if (deal is not null)
        {
            var listing = await listings
                .GetListingDetailsAsync(deal.ListingId, ct)
                .ConfigureAwait(false);
            if (listing is { StayProtectionEnabled: false })
            {
                LogSkippedOptOut(logger, domainEvent.DealId);
                return;
            }
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

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Skipped insurance record for deal {DealId} because the listing opted out of Stay Protection")]
    private static partial void LogSkippedOptOut(ILogger logger, Guid dealId);
}
