using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record CreateHostAccountUpdateLinkCommand(
    Guid HostUserId,
    Uri? ReturnUrl = null,
    Uri? RefreshUrl = null) : IRequest<Result<Uri>>;

public sealed class CreateHostAccountUpdateLinkCommandHandler(
    IdentityDbContext dbContext,
    IStripeService stripeService)
    : IRequestHandler<CreateHostAccountUpdateLinkCommand, Result<Uri>>
{
    public async Task<Result<Uri>> Handle(
        CreateHostAccountUpdateLinkCommand request,
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
                .CreateAccountUpdateLinkAsync(
                    account.StripeAccountId,
                    request.ReturnUrl,
                    request.RefreshUrl,
                    cancellationToken)
                .ConfigureAwait(false);

            return Result<Uri>.Success(url);
        }
        catch (StripeException ex) when (StripeConnectErrors.IsMissingAccount(ex))
        {
            return Result<Uri>.Failure(
                new Error("HostStripe.NotFound", "No Stripe account found. Please start onboarding first."));
        }
        catch (StripeException ex)
        {
            return Result<Uri>.Failure(
                new Error(
                    "HostStripe.ProviderError",
                    string.IsNullOrWhiteSpace(ex.StripeError?.Message)
                        ? "Could not open Stripe account update. Please try again."
                        : ex.StripeError.Message));
        }
    }
}
