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

        var url = await stripeService
            .CreateAccountOnboardingLinkAsync(
                account.StripeAccountId,
                request.ReturnUrl,
                request.RefreshUrl,
                cancellationToken)
            .ConfigureAwait(false);

        return Result<Uri>.Success(url);
    }
}
