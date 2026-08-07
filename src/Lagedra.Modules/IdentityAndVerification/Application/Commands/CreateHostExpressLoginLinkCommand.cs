using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record CreateHostExpressLoginLinkCommand(Guid HostUserId) : IRequest<Result<Uri>>;

public sealed class CreateHostExpressLoginLinkCommandHandler(
    IdentityDbContext dbContext,
    IStripeService stripeService)
    : IRequestHandler<CreateHostExpressLoginLinkCommand, Result<Uri>>
{
    public async Task<Result<Uri>> Handle(
        CreateHostExpressLoginLinkCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = await dbContext.HostStripeAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HostUserId == request.HostUserId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result<Uri>.Failure(
                new Error("HostStripe.NotFound", "No Stripe account found. Please start onboarding first."));
        }

        try
        {
            var url = await stripeService
                .CreateExpressLoginLinkAsync(account.StripeAccountId, cancellationToken)
                .ConfigureAwait(false);
            return Result<Uri>.Success(url);
        }
        catch (StripeException ex) when (
            string.Equals(ex.StripeError?.Code, "account_invalid", StringComparison.Ordinal)
            || (ex.Message?.Contains("does not have access to the Express Dashboard", StringComparison.OrdinalIgnoreCase) ?? false)
            || (ex.Message?.Contains("login link", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return Result<Uri>.Failure(
                new Error(
                    "HostStripe.ExpressDashboardUnavailable",
                    "Finish Stripe onboarding first, then you can open the Express Dashboard."));
        }
    }
}
