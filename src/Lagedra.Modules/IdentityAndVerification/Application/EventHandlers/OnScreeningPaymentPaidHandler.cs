using Lagedra.Modules.IdentityAndVerification.Application.Commands;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Events;
using Lagedra.SharedKernel.Integration;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.EventHandlers;

public sealed class OnScreeningPaymentPaidHandler(
    IdentityDbContext dbContext,
    IMediator mediator)
    : IDomainEventHandler<ScreeningPaymentPaidEvent>
{
    public async Task Handle(ScreeningPaymentPaidEvent domainEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var order = await dbContext.ScreeningOrders
            .FirstOrDefaultAsync(o => o.Id == domainEvent.ScreeningOrderId, ct)
            .ConfigureAwait(false);
        if (order is null)
        {
            return;
        }

        order.MarkPaid();
        if (!string.IsNullOrWhiteSpace(domainEvent.PaymentIntentId))
        {
            order.RecordPaymentIntent(domainEvent.PaymentIntentId);
        }

        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await mediator.Send(new PlaceProviderScreeningOrderCommand(order.Id), ct).ConfigureAwait(false);
    }
}
