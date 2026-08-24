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
    string Email,
    Uri? ReturnUrl = null,
    Uri? RefreshUrl = null) : IRequest<Result<HostStripeStatusDto>>;

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
            var stripeStatus = await SyncFromStripe(existing, cancellationToken).ConfigureAwait(false);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // Already fully enabled — do not send the host back through Stripe
            // (Account Links for complete accounts redirect immediately → UI loop).
            if (existing.ChargesEnabled && existing.PayoutsEnabled)
            {
                return Result<HostStripeStatusDto>.Success(
                    SyncHostStripeStatusCommandHandler.MapToDto(existing, null, stripeStatus));
            }

            // Details already submitted but payouts flipped off (common when
            // Stripe later asks for phone / identity). account_onboarding
            // immediately returns them here; only account_update can collect
            // the new fields. If nothing is currently due, Stripe is reviewing
            // — sending any link just loops.
            if (stripeStatus.DetailsSubmitted && !stripeStatus.HasActionableRequirements)
            {
                return Result<HostStripeStatusDto>.Success(
                    SyncHostStripeStatusCommandHandler.MapToDto(existing, null, stripeStatus));
            }

            var link = await stripeService
                .CreateConnectActionLinkAsync(
                    existing.StripeAccountId,
                    request.ReturnUrl,
                    request.RefreshUrl,
                    cancellationToken)
                .ConfigureAwait(false);

            return Result<HostStripeStatusDto>.Success(
                SyncHostStripeStatusCommandHandler.MapToDto(existing, link, stripeStatus));
        }

        var result = await stripeService
            .CreateConnectedAccountAsync(
                request.HostUserId,
                request.Email,
                request.ReturnUrl,
                request.RefreshUrl,
                cancellationToken)
            .ConfigureAwait(false);

        var account = HostStripeAccount.Create(request.HostUserId, result.AccountId, clock);
        dbContext.HostStripeAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<HostStripeStatusDto>.Success(
            SyncHostStripeStatusCommandHandler.MapToDto(account, result.OnboardingUrl));
    }

    private async Task<StripeAccountStatusResult> SyncFromStripe(
        HostStripeAccount account,
        CancellationToken ct)
    {
        var status = await stripeService
            .GetAccountStatusAsync(account.StripeAccountId, ct)
            .ConfigureAwait(false);

        account.SyncStatus(
            status.ChargesEnabled,
            status.PayoutsEnabled,
            status.DetailsSubmitted,
            status.HasExternalAccount,
            status.HasOutstandingTaxRequirement,
            status.TaxRequirementPastDue,
            status.TaxRequirementPendingVerification,
            status.IsRestricted,
            status.HasOutstandingBankRequirement,
            status.BankRequirementPastDue,
            clock);

        return status;
    }
}
