using Lagedra.Modules.PartnerNetwork.Application.DTOs;
using Lagedra.Modules.PartnerNetwork.Domain.Entities;
using Lagedra.Modules.PartnerNetwork.Domain.Enums;
using Lagedra.Modules.PartnerNetwork.Infrastructure.Persistence;
using Lagedra.SharedKernel.Results;
using Lagedra.SharedKernel.Time;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Lagedra.Modules.PartnerNetwork.Application.Commands;

public sealed record CreateDirectReservationCommand(
    Guid OrganizationId,
    string GuestName,
    string GuestEmail,
    Guid ListingId,
    Guid ReservedByUserId) : IRequest<Result<DirectReservationDto>>;

public sealed class CreateDirectReservationCommandHandler(
    PartnerDbContext dbContext,
    IClock clock)
    : IRequestHandler<CreateDirectReservationCommand, Result<DirectReservationDto>>
{
    public async Task<Result<DirectReservationDto>> Handle(
        CreateDirectReservationCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var org = await dbContext.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);

        if (org is null)
        {
            return Result<DirectReservationDto>.Failure(
                new Error("Partner.NotFound", "Partner organization not found."));
        }

        if (org.Status != PartnerOrganizationStatus.Verified)
        {
            return Result<DirectReservationDto>.Failure(
                new Error("Partner.NotVerified",
                    "Only verified partner organizations can create direct reservations."));
        }

        var reservation = DirectReservation.Create(
            request.OrganizationId, request.GuestName, request.GuestEmail,
            request.ListingId, request.ReservedByUserId, clock);

        dbContext.DirectReservations.Add(reservation);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<DirectReservationDto>.Success(
            new DirectReservationDto(reservation.Id, reservation.OrganizationId,
                reservation.GuestName, reservation.GuestEmail, reservation.ListingId,
                reservation.DealApplicationId, reservation.ReservedByUserId, reservation.CreatedAt));
    }
}
