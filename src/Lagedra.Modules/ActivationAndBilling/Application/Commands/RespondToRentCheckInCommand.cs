using Lagedra.Modules.ActivationAndBilling.Application.DTOs;
using Lagedra.Modules.ActivationAndBilling.Domain.Enums;
using Lagedra.Modules.ActivationAndBilling.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.ActivationAndBilling.Application.Commands;

/// <summary>
/// Host answers a monthly rent check-in: received, or not received. "Not
/// received" raises RentMissedEvent, which the compliance module records as
/// a PaymentDefault signal (violations/escalations follow its existing
/// scanner rules).
/// </summary>
public sealed record RespondToRentCheckInCommand(
    Guid DealId,
    Guid CheckInId,
    Guid CallerUserId,
    bool Received,
    string? Note) : IRequest<Result<RentCheckInDto>>;

public sealed class RespondToRentCheckInCommandHandler(
    BillingDbContext dbContext,
    IClock clock)
    : IRequestHandler<RespondToRentCheckInCommand, Result<RentCheckInDto>>
{
    public async Task<Result<RentCheckInDto>> Handle(
        RespondToRentCheckInCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var checkIn = await dbContext.RentCheckIns
            .FirstOrDefaultAsync(
                r => r.Id == request.CheckInId && r.DealId == request.DealId,
                cancellationToken)
            .ConfigureAwait(false);

        if (checkIn is null)
        {
            return Result<RentCheckInDto>.Failure(
                new Error("RentCheckIn.NotFound", "Rent check-in not found for this deal."));
        }

        if (checkIn.LandlordUserId != request.CallerUserId)
        {
            return Result<RentCheckInDto>.Failure(
                new Error("RentCheckIn.Forbidden", "Only the host can answer a rent check-in."));
        }

        if (checkIn.Status != RentCheckInStatus.Pending)
        {
            return Result<RentCheckInDto>.Failure(
                new Error("RentCheckIn.AlreadyAnswered", "This rent check-in has already been answered."));
        }

        if (request.Received)
        {
            checkIn.MarkReceived(request.Note, clock);
        }
        else
        {
            checkIn.MarkMissed(request.Note, clock);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<RentCheckInDto>.Success(new RentCheckInDto(
            checkIn.Id, checkIn.DealId, checkIn.PeriodStart, checkIn.PeriodEnd,
            checkIn.Status, checkIn.RespondedAt, checkIn.Note));
    }
}
