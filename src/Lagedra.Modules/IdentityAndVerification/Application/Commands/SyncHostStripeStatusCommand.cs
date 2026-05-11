using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record SyncHostStripeStatusCommand(string StripeAccountId) : IRequest<Result>;

public sealed record SyncHostStripeStatusByUserCommand(Guid HostUserId) : IRequest<Result<HostStripeStatusDto>>;

public sealed class SyncHostStripeStatusCommandHandler(
    IdentityDbContext dbContext,
    IStripeService stripeService,
    IClock clock)
    : IRequestHandler<SyncHostStripeStatusCommand, Result>,
      IRequestHandler<SyncHostStripeStatusByUserCommand, Result<HostStripeStatusDto>>
{
    public async Task<Result> Handle(SyncHostStripeStatusCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = await dbContext.HostStripeAccounts
            .FirstOrDefaultAsync(h => h.StripeAccountId == request.StripeAccountId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result.Success();
        }

        await SyncFromStripe(account, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    public async Task<Result<HostStripeStatusDto>> Handle(SyncHostStripeStatusByUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var account = await dbContext.HostStripeAccounts
            .FirstOrDefaultAsync(h => h.HostUserId == request.HostUserId, cancellationToken)
            .ConfigureAwait(false);

        if (account is null)
        {
            return Result<HostStripeStatusDto>.Failure(
                new Error("HostStripe.NotFound", "No Stripe account found for this host."));
        }

        await SyncFromStripe(account, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<HostStripeStatusDto>.Success(
            new HostStripeStatusDto(account.Id, account.HostUserId, account.StripeAccountId,
                account.OnboardingStatus, account.ChargesEnabled, account.PayoutsEnabled, null));
    }

    private async Task SyncFromStripe(HostStripeAccount account, CancellationToken ct)
    {
        var status = await stripeService
            .GetAccountStatusAsync(account.StripeAccountId, ct)
            .ConfigureAwait(false);

        account.SyncStatus(status.ChargesEnabled, status.PayoutsEnabled, status.DetailsSubmitted, clock);
    }
}
