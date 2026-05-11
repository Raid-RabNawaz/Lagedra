using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.IdentityAndVerification.Application.Commands;

public sealed record CreateHostStripeAccountCommand(
    Guid HostUserId,
    string Email) : IRequest<Result<HostStripeStatusDto>>;

public sealed class CreateHostStripeAccountCommandHandler(
    IdentityDbContext dbContext,
    IStripeService stripeService,
    IClock clock)
    : IRequestHandler<CreateHostStripeAccountCommand, Result<HostStripeStatusDto>>
{
    public async Task<Result<HostStripeStatusDto>> Handle(
        CreateHostStripeAccountCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = await dbContext.HostStripeAccounts
            .FirstOrDefaultAsync(h => h.HostUserId == request.HostUserId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var link = await stripeService
                .CreateAccountOnboardingLinkAsync(existing.StripeAccountId, ct: cancellationToken)
                .ConfigureAwait(false);

            return Result<HostStripeStatusDto>.Success(MapToDto(existing, link));
        }

        var result = await stripeService
            .CreateConnectedAccountAsync(request.HostUserId, request.Email, cancellationToken)
            .ConfigureAwait(false);

        var account = HostStripeAccount.Create(request.HostUserId, result.AccountId, clock);
        dbContext.HostStripeAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<HostStripeStatusDto>.Success(MapToDto(account, result.OnboardingUrl));
    }

    private static HostStripeStatusDto MapToDto(HostStripeAccount a, Uri? onboardingUrl) =>
        new(a.Id, a.HostUserId, a.StripeAccountId, a.OnboardingStatus,
            a.ChargesEnabled, a.PayoutsEnabled, onboardingUrl);
}
