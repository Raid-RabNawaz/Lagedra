using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record CreateStripeCustomerCommand(
    Guid DealId,
    string StripeCustomerId) : IRequest<Result>;

public sealed class CreateStripeCustomerCommandHandler(
    BillingDbContext dbContext)
    : IRequestHandler<CreateStripeCustomerCommand, Result>
{
    public async Task<Result> Handle(
        CreateStripeCustomerCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = await dbContext.BillingAccounts
            .FirstOrDefaultAsync(b => b.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result.Failure(
                new Error("BillingAccount.NotFound", "Billing account not found for this deal."));
        }

        account.SetStripeCustomerId(request.StripeCustomerId);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
