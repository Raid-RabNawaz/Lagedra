using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

public sealed record ConfirmHostPlatformPaymentCommand(
    Guid DealId,
    Guid HostUserId) : IRequest<Result<PaymentConfirmationDto>>;

public sealed class ConfirmHostPlatformPaymentCommandHandler(
    BillingDbContext dbContext,
    IClock clock)
    : IRequestHandler<ConfirmHostPlatformPaymentCommand, Result<PaymentConfirmationDto>>
{
    public async Task<Result<PaymentConfirmationDto>> Handle(
        ConfirmHostPlatformPaymentCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var application = await dbContext.DealApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (application is null || application.LandlordUserId != request.HostUserId)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("PaymentConfirmation.Forbidden",
                    "Only the listing host can confirm the platform fee for this deal."));
        }

        var confirmation = await dbContext.DealPaymentConfirmations
            .FirstOrDefaultAsync(c => c.DealId == request.DealId, cancellationToken)
            .ConfigureAwait(false);

        if (confirmation is null)
        {
            return Result<PaymentConfirmationDto>.Failure(
                new Error("PaymentConfirmation.NotFound", "Payment confirmation record not found."));
        }

        confirmation.ConfirmHostPlatformPayment(clock);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<PaymentConfirmationDto>.Success(
            PaymentConfirmationDtoMapper.ToDto(confirmation));
    }
}
