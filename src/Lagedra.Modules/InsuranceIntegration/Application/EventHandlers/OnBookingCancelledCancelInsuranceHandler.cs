using Lagedra.Modules.ActivationAndBilling.Domain.Events;
using Lagedra.Modules.InsuranceIntegration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.InsuranceIntegration.Application.EventHandlers;

public sealed partial class OnBookingCancelledCancelInsuranceHandler(
    InsuranceDbContext dbContext,
    ILogger<OnBookingCancelledCancelInsuranceHandler> logger)
    : IDomainEventHandler<BookingCancelledEvent>
{
    public async Task Handle(BookingCancelledEvent domainEvent, CancellationToken ct = default)
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

        LogCancellingInsurance(logger, domainEvent.DealId, domainEvent.Reason);

        record.CancelPolicy($"Booking cancelled: {domainEvent.Reason}");

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Cancelling insurance for deal {DealId}: {Reason}")]
    private static partial void LogCancellingInsurance(ILogger logger, Guid dealId, string reason);

    [LoggerMessage(Level = LogLevel.Debug,
        Message = "No insurance policy record found for deal {DealId} — nothing to cancel")]
    private static partial void LogNoPolicyFound(ILogger logger, Guid dealId);
}
