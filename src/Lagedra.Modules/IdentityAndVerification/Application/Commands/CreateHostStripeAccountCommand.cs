using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.IdentityAndVerification.Application.DTOs;
using Lagedra.Modules.IdentityAndVerification.Domain.Entities;
using Lagedra.Modules.IdentityAndVerification.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Stripe;

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

        var existing = await FindIncludingDeleted(request.HostUserId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null && !existing.IsDeleted)
        {
            try
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
            catch (StripeException ex) when (StripeConnectErrors.IsMissingAccount(ex))
            {
                return await RecreateOnExistingRow(existing, request, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (StripeException ex)
            {
                return ProviderError<HostStripeStatusDto>(ex, "Stripe could not start payout setup. Please try again.");
            }
        }

        if (existing is not null)
        {
            return await RecreateOnExistingRow(existing, request, cancellationToken)
                .ConfigureAwait(false);
        }

        return await CreateFreshRow(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<HostStripeStatusDto>> RecreateOnExistingRow(
        HostStripeAccount existing,
        CreateHostStripeAccountCommand request,
        CancellationToken cancellationToken)
    {
        var created = await CreateExpressAccount(request, cancellationToken).ConfigureAwait(false);
        if (created.IsFailure)
        {
            return Result<HostStripeStatusDto>.Failure(created.Error);
        }

        existing.ReplaceConnectedAccount(created.Value.AccountId, clock);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<HostStripeStatusDto>.Success(
            SyncHostStripeStatusCommandHandler.MapToDto(existing, created.Value.OnboardingUrl));
    }

    private async Task<Result<HostStripeStatusDto>> CreateFreshRow(
        CreateHostStripeAccountCommand request,
        CancellationToken cancellationToken)
    {
        var created = await CreateExpressAccount(request, cancellationToken).ConfigureAwait(false);
        if (created.IsFailure)
        {
            return Result<HostStripeStatusDto>.Failure(created.Error);
        }

        var account = HostStripeAccount.Create(request.HostUserId, created.Value.AccountId, clock);
        dbContext.HostStripeAccounts.Add(account);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsHostUserIdConflict(ex))
        {
            // Soft-deleted leftover or a concurrent onboard already inserted.
            dbContext.ChangeTracker.Clear();
            var conflict = await FindIncludingDeleted(request.HostUserId, cancellationToken)
                .ConfigureAwait(false);
            if (conflict is null)
            {
                return Result<HostStripeStatusDto>.Failure(
                    new Error(
                        "HostStripe.ProviderError",
                        "Payout setup could not be saved. Please try again."));
            }

            conflict.ReplaceConnectedAccount(created.Value.AccountId, clock);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            account = conflict;
        }

        return Result<HostStripeStatusDto>.Success(
            SyncHostStripeStatusCommandHandler.MapToDto(account, created.Value.OnboardingUrl));
    }

    private async Task<Result<StripeConnectedAccountResult>> CreateExpressAccount(
        CreateHostStripeAccountCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await stripeService
                .CreateConnectedAccountAsync(
                    request.HostUserId,
                    request.Email,
                    request.ReturnUrl,
                    request.RefreshUrl,
                    cancellationToken)
                .ConfigureAwait(false);
            return Result<StripeConnectedAccountResult>.Success(result);
        }
        catch (StripeException ex)
        {
            return ProviderError<StripeConnectedAccountResult>(ex, "Stripe could not start payout setup. Please try again.");
        }
    }

    private Task<HostStripeAccount?> FindIncludingDeleted(
        Guid hostUserId,
        CancellationToken cancellationToken) =>
        dbContext.HostStripeAccounts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(h => h.HostUserId == hostUserId, cancellationToken);

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

    private static bool IsHostUserIdConflict(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner is not null; inner = inner.InnerException)
        {
            if (inner is PostgresException postgres
                && postgres.SqlState == PostgresErrorCodes.UniqueViolation
                && (postgres.ConstraintName?.Contains("HostUserId", StringComparison.Ordinal) == true
                    || postgres.MessageText.Contains(
                        "IX_host_stripe_accounts_HostUserId",
                        StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return ex.Message.Contains("IX_host_stripe_accounts_HostUserId", StringComparison.Ordinal);
    }

    private static Result<T> ProviderError<T>(StripeException ex, string fallback) =>
        Result<T>.Failure(
            new Error(
                "HostStripe.ProviderError",
                string.IsNullOrWhiteSpace(ex.StripeError?.Message)
                    ? fallback
                    : ex.StripeError.Message));
}
