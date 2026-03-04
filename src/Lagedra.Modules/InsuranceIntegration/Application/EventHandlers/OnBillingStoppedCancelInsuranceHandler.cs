using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed partial class OnBillingStoppedCancelInsuranceHandler(
    InsuranceDbContext dbContext,
    ILogger<OnBillingStoppedCancelInsuranceHandler> logger)
    : IDomainEventHandler<BillingStoppedEvent>
{
    public async Task Handle(BillingStoppedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var record = await dbContext.PolicyRecords
            .FirstOrDefaultAsync(r => r.DealId == domainEvent.DealId, ct)
            .ConfigureAwait(false);

        if (record is null)
        {
            LogNoPolicyFound(logger, domainEvent.DealId);
            return;
        }

        LogCancellingInsurance(logger, domainEvent.DealId);

        record.CancelPolicy("Billing stopped — deal closed");

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Cancelling insurance for deal {DealId}: billing stopped")]
    private static partial void LogCancellingInsurance(ILogger logger, Guid dealId);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "No insurance policy record found for deal {DealId} — nothing to cancel")]
    private static partial void LogNoPolicyFound(ILogger logger, Guid dealId);
}
