using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

/// <summary>
/// Returns the host's free-text payout instructions to the tenant. Under the
/// non-custodial model (Option A) the initial booking — first month's rent +
/// deposit + fees — is charged through Stripe (destination charge to the host's
/// connected account), so these instructions are <b>not</b> the deposit or
/// first-payment channel. They tell the tenant how to pay the host directly for
/// <b>months 2+ rent</b>, which never flows through the platform — so they must
/// stay visible for the whole life of an active (Confirmed) deal, not just
/// while the first payment is Pending.
/// </summary>
public sealed record GetPaymentDetailsForTenantQuery(
    Guid DealId,
    Guid TenantUserId) : IRequest<Result<PaymentDetailsDto>>;

public sealed class GetPaymentDetailsForTenantQueryHandler(
    BillingDbContext dbContext,
    IHostPaymentDetailsProvider paymentDetailsProvider)
    : IRequestHandler<GetPaymentDetailsForTenantQuery, Result<PaymentDetailsDto>>
{
    public async Task<Result<PaymentDetailsDto>> Handle(
        GetPaymentDetailsForTenantQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var confirmation = await dbContext.DealPaymentConfirmations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result<PaymentDetailsDto>.Failure(
                new Error("PaymentConfirmation.NotFound",
                    "No payment confirmation record found for this deal."));
        }

        // Pending: awaiting the first payment. Confirmed: active deal, where
        // the tenant needs these details every month for direct rent. Only
        // dead deals (rejected/cancelled) or disputed ones hide them.
        if (confirmation.Status is not (PaymentConfirmationStatus.Pending or PaymentConfirmationStatus.Confirmed))
        {
            return Result<PaymentDetailsDto>.Failure(
                new Error("PaymentConfirmation.InvalidState",
                    "Payment details are not available for this deal's current state."));
        }

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null || application.TenantUserId != request.TenantUserId)
        {
            return Result<PaymentDetailsDto>.Failure(
                new Error("PaymentDetails.Forbidden",
                    "You are not authorized to view payment details for this deal."));
        }

        var hostDetails = await paymentDetailsProvider
            .GetDecryptedPaymentDetailsAsync(application.LandlordUserId, cancellationToken)
            .ConfigureAwait(false);

        if (hostDetails is null)
        {
            return Result<PaymentDetailsDto>.Failure(
                new Error("PaymentDetails.NotConfigured",
                    "Host has not configured payment details yet."));
        }

        return Result<PaymentDetailsDto>.Success(
            new PaymentDetailsDto(request.DealId, hostDetails.PaymentInfo));
    }
}
