using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record RefreshHostOnboardingLinkCommand(
    Guid HostUserId,
    Uri? ReturnUrl = null,
    Uri? RefreshUrl = null) : IRequest<Result<Uri>>;

public sealed class RefreshHostOnboardingLinkCommandHandler(
    IdentityDbContext dbContext,
    IStripeService stripeService)
    : IRequestHandler<RefreshHostOnboardingLinkCommand, Result<Uri>>
{
    public async Task<Result<Uri>> Handle(
        RefreshHostOnboardingLinkCommand request,
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

        var status = await stripeService
            .GetAccountStatusAsync(account.StripeAccountId, cancellationToken)
            .ConfigureAwait(false);

        if (status.ChargesEnabled && status.PayoutsEnabled)
        {
            return Result<Uri>.Failure(
                new Error("HostStripe.AlreadyEnabled",
                    "Your Stripe account is already enabled. Open Stripe Express to view it."));
        }

        if (status.DetailsSubmitted && !status.HasActionableRequirements)
        {
            return Result<Uri>.Failure(
                new Error("HostStripe.PendingReview",
                    "Stripe is still reviewing your information. Nothing more can be submitted right now."));
        }

        var url = await stripeService
            .CreateConnectActionLinkAsync(
                account.StripeAccountId,
                request.ReturnUrl,
                request.RefreshUrl,
                cancellationToken)
            .ConfigureAwait(false);

        return Result<Uri>.Success(url);
    }
}
