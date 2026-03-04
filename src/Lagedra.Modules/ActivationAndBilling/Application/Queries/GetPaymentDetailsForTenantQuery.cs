using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Integration;
using Lagedra.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Queries;

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

        if (confirmation.Status != PaymentConfirmationStatus.Pending)
        {
            return Result<PaymentDetailsDto>.Failure(
                new Error("PaymentConfirmation.InvalidState",
                    "Payment details are only available while awaiting confirmation."));
        }

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null || application.TenantUserId != request.TenantUserId)
        {
            return Result<PaymentDetailsDto>.Failure(
                new Error("PaymentDetails.Unauthorized",
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
