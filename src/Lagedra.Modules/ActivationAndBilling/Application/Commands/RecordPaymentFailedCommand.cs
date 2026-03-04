using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record RecordPaymentFailedCommand(
    Guid InvoiceId) : IRequest<Result>;

public sealed class RecordPaymentFailedCommandHandler(
    BillingDbContext dbContext)
    : IRequestHandler<RecordPaymentFailedCommand, Result>
{
    public async Task<Result> Handle(
        RecordPaymentFailedCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invoice = await dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == request.InvoiceId, cancellationToken)
            .ConfigureAwait(false);

        if (invoice is null)
        {
            return Result.Failure(new Error("Invoice.NotFound", "Invoice not found."));
        }

        invoice.MarkFailed();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
