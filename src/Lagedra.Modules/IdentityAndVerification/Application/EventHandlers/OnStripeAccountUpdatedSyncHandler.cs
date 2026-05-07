using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lagedra.Modules.IdentityAndVerification.Application.EventHandlers;

public sealed partial class OnStripeAccountUpdatedSyncHandler(
    IMediator mediator,
    ILogger<OnStripeAccountUpdatedSyncHandler> logger)
    : IDomainEventHandler<StripeAccountUpdatedEvent>
{
    public async Task Handle(StripeAccountUpdatedEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        LogSyncing(logger, domainEvent.StripeAccountId);

        await mediator
            .Send(new SyncHostStripeStatusCommand(domainEvent.StripeAccountId), ct)
            .ConfigureAwait(false);
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Syncing Stripe account status for connected account {StripeAccountId}")]
    private static partial void LogSyncing(ILogger logger, string stripeAccountId);
}
