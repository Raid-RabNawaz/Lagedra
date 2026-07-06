using Lagedra.Infrastructure.External.Payments;
using Lagedra.Modules.Arbitration.Application.DTOs;
using Lagedra.Modules.Arbitration.Domain.Enums;
using Lagedra.Modules.Arbitration.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.Arbitration.Application.Commands;

/// <summary>
/// Creates (or resumes) the Stripe checkout for an arbitration filing fee. Only
/// the filer can pay, and only while the case is awaiting payment. The fee is
/// the platform's own adjudication revenue, so it is a plain platform charge.
/// </summary>
public sealed record CreateArbitrationFeeCheckoutCommand(
    Guid CaseId,
    Guid RequestingUserId) : IRequest<Result<ArbitrationFeeCheckoutDto>>;

public sealed class CreateArbitrationFeeCheckoutCommandHandler(
    ArbitrationDbContext dbContext,
    IStripeService stripeService)
    : IRequestHandler<CreateArbitrationFeeCheckoutCommand, Result<ArbitrationFeeCheckoutDto>>
{
    private const string Currency = "usd";

    private static readonly string[] PayableStatuses =
        ["requires_payment_method", "requires_confirmation", "requires_action"];

    public async Task<Result<ArbitrationFeeCheckoutDto>> Handle(
        CreateArbitrationFeeCheckoutCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var arbitrationCase = await dbContext.ArbitrationCases
            .FirstOrDefaultAsync(c => c.Id == request.CaseId, cancellationToken)
            .ConfigureAwait(false);

        if (arbitrationCase is null)
        {
            return Result<ArbitrationFeeCheckoutDto>.Failure(
                new Error("Arbitration.CaseNotFound", "Arbitration case not found."));
        }

        if (arbitrationCase.FiledByUserId != request.RequestingUserId)
        {
            return Result<ArbitrationFeeCheckoutDto>.Failure(
                new Error("Arbitration.Forbidden", "Only the party who filed the case can pay the filing fee."));
        }

        if (arbitrationCase.Status != ArbitrationStatus.PendingPayment)
        {
            return Result<ArbitrationFeeCheckoutDto>.Failure(
                new Error("Arbitration.FeeNotPending", "This case is not awaiting a filing-fee payment."));
        }

        if (arbitrationCase.FilingFeeCents <= 0)
        {
            // Defensive: a zero-fee case should never be PendingPayment, but if it
            // somehow is, activate it rather than create a $0 PaymentIntent.
            arbitrationCase.MarkFilingFeePaid();
            await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
            return Result<ArbitrationFeeCheckoutDto>.Failure(
                new Error("Arbitration.FeeNotPending", "This case has no filing fee due."));
        }

        // Decouple the Stripe call from the HTTP request token: if the client
        // disconnects we still want the PaymentIntent created and persisted so a
        // page refresh resumes the same checkout.
        using var stripeCts = new CancellationTokenSource(TimeSpan.FromSeconds(55));

        if (!string.IsNullOrEmpty(arbitrationCase.FilingFeePaymentIntentId))
        {
            var existing = await stripeService
                .GetPaymentIntentAsync(arbitrationCase.FilingFeePaymentIntentId, stripeCts.Token)
                .ConfigureAwait(false);

            if (existing.Status is "succeeded")
            {
                arbitrationCase.MarkFilingFeePaid();
                await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
                return Result<ArbitrationFeeCheckoutDto>.Success(Build(existing, arbitrationCase));
            }

            if (PayableStatuses.Contains(existing.Status))
            {
                return Result<ArbitrationFeeCheckoutDto>.Success(Build(existing, arbitrationCase));
            }
        }

        var metadata = new Dictionary<string, string>
        {
            [ArbitrationFeePaymentMetadata.PurposeKey] = ArbitrationFeePaymentMetadata.PurposeValue,
            [ArbitrationFeePaymentMetadata.CaseIdKey] = arbitrationCase.Id.ToString(),
            [ArbitrationFeePaymentMetadata.DealIdKey] = arbitrationCase.DealId.ToString(),
            [ArbitrationFeePaymentMetadata.FiledByUserIdKey] = arbitrationCase.FiledByUserId.ToString()
        };

        var pi = await stripeService.CreatePlatformPaymentIntentAsync(
            arbitrationCase.FilingFeeCents,
            Currency,
            metadata,
            idempotencyKey: $"arb-fee-{arbitrationCase.Id}",
            stripeCts.Token).ConfigureAwait(false);

        arbitrationCase.RecordFilingFeePaymentIntent(pi.PaymentIntentId);
        await dbContext.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);

        return Result<ArbitrationFeeCheckoutDto>.Success(Build(pi, arbitrationCase));
    }

    private static ArbitrationFeeCheckoutDto Build(StripePaymentIntentResult pi, Domain.Aggregates.ArbitrationCase c) =>
        new(pi.ClientSecret, pi.PaymentIntentId, pi.Status, c.FilingFeeCents, Currency, c.Status);
}
