using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.Arbitration.Application.EventHandlers;

/// <summary>
/// Activates an arbitration case once its filing-fee PaymentIntent has succeeded.
/// Driven by the Stripe webhook (ActivationAndBilling) via a shared integration
/// event so the two modules stay decoupled. Idempotent against duplicate webhook
/// deliveries.
/// </summary>
public sealed partial class OnArbitrationFilingFeePaidHandler(
    ArbitrationDbContext db,
    ILogger<OnArbitrationFilingFeePaidHandler> logger)
    : IDomainEventHandler<ArbitrationFilingFeePaidEvent>
{
    public async Task Handle(ArbitrationFilingFeePaidEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var arbitrationCase = await db.ArbitrationCases
            .FirstOrDefaultAsync(c => c.Id == domainEvent.CaseId, ct)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            LogCaseMissing(logger, domainEvent.CaseId, domainEvent.PaymentIntentId);
            return;
        }

        arbitrationCase.MarkFilingFeePaid();
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        LogCaseActivated(logger, domainEvent.CaseId, domainEvent.PaymentIntentId);
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Arbitration filing-fee paid for unknown case {CaseId} (PI {PaymentIntentId})")]
    private static partial void LogCaseMissing(ILogger logger, Guid caseId, string paymentIntentId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Arbitration case {CaseId} activated after filing-fee payment (PI {PaymentIntentId})")]
    private static partial void LogCaseActivated(ILogger logger, Guid caseId, string paymentIntentId);
}
