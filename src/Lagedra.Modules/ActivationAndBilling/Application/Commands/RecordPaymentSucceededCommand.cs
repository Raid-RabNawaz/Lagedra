using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record RecordPaymentSucceededCommand(
    Guid InvoiceId) : IRequest<Result>;

public sealed class RecordPaymentSucceededCommandHandler(
    BillingDbContext dbContext)
    : IRequestHandler<RecordPaymentSucceededCommand, Result>
{
    public async Task<Result> Handle(
        RecordPaymentSucceededCommand request,
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

        invoice.MarkPaid();

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
